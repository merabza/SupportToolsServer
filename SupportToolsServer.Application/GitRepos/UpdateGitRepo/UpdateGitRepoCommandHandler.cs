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

namespace SupportToolsServer.Application.GitRepos.UpdateGitRepo;

public class UpdateGitRepoCommandHandler : ICommandHandler<UpdateGitRepoCommand>
{
    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;
    private readonly IGitRepoRepository _gitRepoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGitRepoCommandHandler(IGitRepoRepository gitRepoRepository,
        IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository, IUnitOfWork unitOfWork)
    {
        _gitRepoRepository = gitRepoRepository;
        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateGitRepoCommand command, CancellationToken cancellationToken)
    {
        StsGitDataModel model = command.GitRepo;

        GitIgnoreFileType? gitIgnoreFileType =
            await _gitIgnoreFileTypeRepository.GetByName(model.GitIgnorePatternName, cancellationToken);
        if (gitIgnoreFileType is null)
        {
            return SupportToolsServerApiClientErrors.GitIgnoreFileTypeWithNameNotFound(model.GitIgnorePatternName);
        }

        List<GitRepo> gitRepos = await _gitRepoRepository.GetAll(cancellationToken);
        GitRepo? existingGitRepo = gitRepos.SingleOrDefault(x =>
            string.Equals(x.Name, model.GitProjectName, StringComparison.OrdinalIgnoreCase));

        //მისამართი უნიკალურია, ამიტომ სხვა რეპოზიტორიის მისამართი ბაზის ინდექსამდე უარიყოფა
        GitRepo? addressOwner = gitRepos.FirstOrDefault(x =>
            !string.Equals(x.Name, model.GitProjectName, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Address,
                model.GitProjectAddress, StringComparison.OrdinalIgnoreCase));
        if (addressOwner is not null)
        {
            return SupportToolsServerApiClientErrors.GitAddressIsInUse(model.GitProjectAddress, addressOwner.Name);
        }

        //დამატება და რედაქტირება დომენის მოვლენებს აგენერირებს, რომლებიც შენახვის შემდეგ იგზავნება
        if (existingGitRepo is null)
        {
            _gitRepoRepository.Add(GitRepo.Create(model.GitProjectName, model.GitProjectAddress,
                model.GitProjectFolderName, gitIgnoreFileType.Id));
        }
        else
        {
            existingGitRepo.Update(model.GitProjectName, model.GitProjectAddress, model.GitProjectFolderName,
                gitIgnoreFileType.Id);
            _gitRepoRepository.Update(existingGitRepo);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
