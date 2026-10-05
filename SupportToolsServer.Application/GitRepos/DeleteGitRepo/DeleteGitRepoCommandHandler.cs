using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Projects;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.Projects;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.DeleteGitRepo;

public class DeleteGitRepoCommandHandler : ICommandHandler<DeleteGitRepoCommand>
{
    private readonly IGitRepoRepository _gitRepoRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGitRepoCommandHandler(IGitRepoRepository gitRepoRepository, IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _gitRepoRepository = gitRepoRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteGitRepoCommand command, CancellationToken cancellationToken)
    {
        GitRepo? gitRepo = await _gitRepoRepository.GetByName(command.Key, cancellationToken);
        if (gitRepo is null)
        {
            return SupportToolsServerApiClientErrors.GitWithKeyNotFound(command.Key);
        }

        //git-ს პროექტები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse პროექტების სიით
        List<string> usages = [.. (await _projectRepository.GetAll(cancellationToken)).GetUsages(gitRepo.Id)];
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(GitRepoContractMapper.EntityName, command.Key,
                usages);
        }

        _gitRepoRepository.Delete(gitRepo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
