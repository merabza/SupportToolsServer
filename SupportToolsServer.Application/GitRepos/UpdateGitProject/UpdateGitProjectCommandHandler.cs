using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.GitRepoProjects;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//SupportTools-ის GitOneProjectUpdater.UpdateOneGitProject-ისა და GitProjectsUpdater.ProcessFolder-ის ანალოგი.
//კლიენტისგან განსხვავებით სერვერი ლოკალური ცვლილებების გაუქმებაზე თანხმობას არ ითხოვს: სამუშაო ფოლდერი ყოველთვის
//სერვერის ვერსიას უბრუნდება. წარმატებული განახლების შემდეგ კლონის პროექტები სკანირდება და რეპოზიტორიის შენახულ
//პროექტებს ანაცვლებს (B9). git-ის ან სკანირების შეცდომისას შენახული პროექტები უცვლელი რჩება
public sealed class UpdateGitProjectCommandHandler : ICommandHandler<UpdateGitProjectCommand>
{
    private readonly IGitClient _gitClient;
    private readonly IGitProjectFilesScanner _gitProjectFilesScanner;
    private readonly IGitRepoProjectRepository _gitRepoProjectRepository;
    private readonly IGitsWorkFolder _gitsWorkFolder;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGitProjectCommandHandler(IGitsWorkFolder gitsWorkFolder, IGitClient gitClient,
        IGitProjectFilesScanner gitProjectFilesScanner, IGitRepoProjectRepository gitRepoProjectRepository,
        IUnitOfWork unitOfWork)
    {
        _gitsWorkFolder = gitsWorkFolder;
        _gitClient = gitClient;
        _gitProjectFilesScanner = gitProjectFilesScanner;
        _gitRepoProjectRepository = gitRepoProjectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateGitProjectCommand command, CancellationToken cancellationToken)
    {
        //Gits ფოლდერის გარეთ გასული გზა შეცდომაა, ამიტომ ასეთი ფოლდერი არც იშლება და არც იკლონება
        Result<string> projectFolderPathResult = _gitsWorkFolder.GetProjectFolderPath(GitsFolderName(command));
        if (projectFolderPathResult.IsFailure)
        {
            return projectFolderPathResult.Error;
        }

        string projectFolderPath = projectFolderPathResult.Value;

        Result updateResult = UpdateGitProject(command, projectFolderPath);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        Result<List<ScannedGitProject>> scanResult = _gitProjectFilesScanner.Scan(projectFolderPath);
        if (scanResult.IsFailure)
        {
            return scanResult.Error;
        }

        Result lengthsResult = CheckLengths(scanResult.Value);
        if (lengthsResult.IsFailure)
        {
            return lengthsResult;
        }

        await _gitRepoProjectRepository.Replace(command.GitRepoId,
            scanResult.Value.Select(x => GitRepoProject.Create(command.GitRepoId, x.ProjectRelativePath,
                x.ProjectFileName, x.DependsOnProjectNames)), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private Result UpdateGitProject(UpdateGitProjectCommand command, string projectFolderPath)
    {
        //თუ ფოლდერი არსებობს, მაგრამ სხვა მისამართიდანაა დაკლონილი, წაიშალოს თავისი შიგთავსით
        if (_gitsWorkFolder.Exists(projectFolderPath))
        {
            Result<string> remoteOriginUrlResult = _gitClient.GetRemoteOriginUrl(projectFolderPath);
            if (remoteOriginUrlResult.IsFailure)
            {
                return remoteOriginUrlResult.Error;
            }

            if (remoteOriginUrlResult.Value != command.GitProjectAddress)
            {
                _gitsWorkFolder.Delete(projectFolderPath);
            }
        }

        if (!_gitsWorkFolder.Exists(projectFolderPath))
        {
            return _gitClient.Clone(command.GitProjectAddress, projectFolderPath);
        }

        //იდეაში სამუშაო ფოლდერში ცვლილებები არ უნდა მომხდარიყო, მაგრამ თუ მოხდა, უქმდება
        Result<bool> hasChangesResult = _gitClient.HasChanges(projectFolderPath);
        if (hasChangesResult.IsFailure)
        {
            return hasChangesResult.Error;
        }

        if (hasChangesResult.Value)
        {
            return _gitClient.Restore(projectFolderPath);
        }

        Result remoteUpdateResult = _gitClient.RemoteUpdate(projectFolderPath);
        if (remoteUpdateResult.IsFailure)
        {
            return remoteUpdateResult;
        }

        //ლოკალური და remote ვერსიები თუ განსხვავდება, გაკეთდეს pull
        Result<bool> needPullResult = _gitClient.NeedPull(projectFolderPath);
        if (needPullResult.IsFailure)
        {
            return needPullResult.Error;
        }

        return needPullResult.Value ? _gitClient.Pull(projectFolderPath) : Result.Success();
    }

    //SPA-ის წინსართიანი ფოლდერი კლიენტში SPA პროექტის ფოლდერის მიმართ ითვლება. Gits-ში, როგორც კლიენტის ქეშში
    //(GitRepos.Create, useGitRecordNameForComplexGitProjectFolderName), ასეთი რეპოზიტორია git-ის სახელით ინახება
    private static string GitsFolderName(UpdateGitProjectCommand command)
    {
        return command.GitProjectFolderName.StartsWith(GitRules.SpaProjectFolderRelativePathName,
            StringComparison.Ordinal)
            ? command.GitProjectName
            : command.GitProjectFolderName;
    }

    //სკანირების შედეგი ბაზის სვეტებში უნდა ჩაეტიოს. შეცდომა ფაილს ასახელებს, შენახული პროექტები კი უცვლელი რჩება
    private static Result CheckLengths(List<ScannedGitProject> scannedGitProjects)
    {
        foreach (ScannedGitProject scannedGitProject in scannedGitProjects)
        {
            string filePath = $@"{scannedGitProject.ProjectRelativePath}\{scannedGitProject.ProjectFileName}";
            if (scannedGitProject.ProjectRelativePath.Length > GitRepoProject.ProjectRelativePathMaxLength)
            {
                return SupportToolsServerApiClientErrors.ValueTooLong(
                    $"{filePath}.{nameof(ScannedGitProject.ProjectRelativePath)}",
                    GitRepoProject.ProjectRelativePathMaxLength);
            }

            if (scannedGitProject.ProjectFileName.Length > GitRepoProject.ProjectFileNameMaxLength)
            {
                return SupportToolsServerApiClientErrors.ValueTooLong(
                    $"{filePath}.{nameof(ScannedGitProject.ProjectFileName)}", GitRepoProject.ProjectFileNameMaxLength);
            }

            if (scannedGitProject.DependsOnProjectNames.Any(x =>
                    x.Length > GitRepoProjectDependency.ProjectNameMaxLength))
            {
                return SupportToolsServerApiClientErrors.ValueTooLong(
                    $"{filePath}.{nameof(ScannedGitProject.DependsOnProjectNames)}",
                    GitRepoProjectDependency.ProjectNameMaxLength);
            }
        }

        return Result.Success();
    }
}
