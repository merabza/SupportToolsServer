using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.UploadGitRepos;

//ატვირთვა მხოლოდ ამატებს და ცვლის: gitignore ფაილის ტიპები და რეპოზიტორიები არსებულებს სახელით ემთხვევა
//(რეგისტრის გაუთვალისწინებლად), ატვირთულ სიაში არარსებული ჩანაწერები არ იშლება
public class UploadGitReposCommandHandler : ICommandHandler<UploadGitReposCommand>
{
    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;
    private readonly IGitRepoRepository _gitRepoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadGitReposCommandHandler(IGitRepoRepository gitRepoRepository,
        IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository, IUnitOfWork unitOfWork)
    {
        _gitRepoRepository = gitRepoRepository;
        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UploadGitReposCommand command, CancellationToken cancellationToken)
    {
        Dictionary<string, GitIgnoreFileType> gitIgnoreFileTypesByName =
            (await _gitIgnoreFileTypeRepository.GetAll(cancellationToken)).ToDictionary(x => x.Name,
                StringComparer.OrdinalIgnoreCase);
        (List<GitIgnoreFileType> newGitIgnoreFileTypes, List<GitIgnoreFileType> changedGitIgnoreFileTypes) =
            MergeGitIgnoreFileTypes(gitIgnoreFileTypesByName, command.GitIgnoreFiles);

        string[] unknownPatternNames =
        [
            .. command.Gits.Select(x => x.GitIgnorePatternName)
                .Where(x => !gitIgnoreFileTypesByName.ContainsKey(x)).Distinct(StringComparer.OrdinalIgnoreCase)
        ];
        if (unknownPatternNames.Length > 0)
        {
            return SupportToolsServerApiClientErrors.GitIgnoreFileTypeWithNameNotFound(
                string.Join(", ", unknownPatternNames));
        }

        Dictionary<string, GitRepo> gitReposByName =
            (await _gitRepoRepository.GetAll(cancellationToken)).ToDictionary(x => x.Name,
                StringComparer.OrdinalIgnoreCase);
        (List<GitRepo> newGitRepos, List<GitRepo> changedGitRepos) =
            MergeGitRepos(gitReposByName, gitIgnoreFileTypesByName, command.Gits);

        //მისამართი უნიკალურია: შედეგად ერთი მისამართი ორ რეპოზიტორიას არ უნდა ჰქონდეს
        IGrouping<string, GitRepo>? sharedAddress = gitReposByName.Values
            .GroupBy(x => x.Address, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
        if (sharedAddress is not null)
        {
            return SupportToolsServerApiClientErrors.GitAddressIsInUse(sharedAddress.Key,
                string.Join(", ", sharedAddress.Select(x => x.Name)));
        }

        newGitIgnoreFileTypes.ForEach(_gitIgnoreFileTypeRepository.Add);
        changedGitIgnoreFileTypes.ForEach(_gitIgnoreFileTypeRepository.Update);
        newGitRepos.ForEach(_gitRepoRepository.Add);
        changedGitRepos.ForEach(_gitRepoRepository.Update);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    //gitIgnoreFileTypesByName შედეგშიც ახლდება, რომ რეპოზიტორიებმა ახალი ტიპებიც იპოვონ
    private static (List<GitIgnoreFileType> New, List<GitIgnoreFileType> Changed) MergeGitIgnoreFileTypes(
        Dictionary<string, GitIgnoreFileType> gitIgnoreFileTypesByName,
        List<StsGitIgnoreFileTypeDataModel> gitIgnoreFiles)
    {
        List<GitIgnoreFileType> newGitIgnoreFileTypes = [];
        List<GitIgnoreFileType> changedGitIgnoreFileTypes = [];
        foreach (StsGitIgnoreFileTypeDataModel gitIgnoreFile in gitIgnoreFiles)
        {
            if (gitIgnoreFileTypesByName.TryGetValue(gitIgnoreFile.Name, out GitIgnoreFileType? existing))
            {
                var changed = new GitIgnoreFileType(existing.Id, gitIgnoreFile.Name, gitIgnoreFile.Content);
                changedGitIgnoreFileTypes.Add(changed);
                gitIgnoreFileTypesByName[gitIgnoreFile.Name] = changed;
            }
            else
            {
                var added = new GitIgnoreFileType(GitIgnoreFileTypeId.CreateUnique(), gitIgnoreFile.Name,
                    gitIgnoreFile.Content);
                newGitIgnoreFileTypes.Add(added);
                gitIgnoreFileTypesByName[gitIgnoreFile.Name] = added;
            }
        }

        return (newGitIgnoreFileTypes, changedGitIgnoreFileTypes);
    }

    //gitReposByName შედეგში ატვირთვის შემდგომ მდგომარეობას ასახავს
    private static (List<GitRepo> New, List<GitRepo> Changed) MergeGitRepos(
        Dictionary<string, GitRepo> gitReposByName, Dictionary<string, GitIgnoreFileType> gitIgnoreFileTypesByName,
        List<StsGitDataModel> gits)
    {
        List<GitRepo> newGitRepos = [];
        List<GitRepo> changedGitRepos = [];
        foreach (StsGitDataModel git in gits)
        {
            GitIgnoreFileTypeId gitIgnoreFileTypeId = gitIgnoreFileTypesByName[git.GitIgnorePatternName].Id;
            if (gitReposByName.TryGetValue(git.GitProjectName, out GitRepo? existing))
            {
                var changed = new GitRepo(existing.Id, git.GitProjectName, git.GitProjectAddress,
                    git.GitProjectFolderName, gitIgnoreFileTypeId);
                changedGitRepos.Add(changed);
                gitReposByName[git.GitProjectName] = changed;
            }
            else
            {
                var added = new GitRepo(GitRepoId.CreateUnique(), git.GitProjectName, git.GitProjectAddress,
                    git.GitProjectFolderName, gitIgnoreFileTypeId);
                newGitRepos.Add(added);
                gitReposByName[git.GitProjectName] = added;
            }
        }

        return (newGitRepos, changedGitRepos);
    }
}
