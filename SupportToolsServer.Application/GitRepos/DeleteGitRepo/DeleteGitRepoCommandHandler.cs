using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.DeleteGitRepo;

public class DeleteGitRepoCommandHandler : ICommandHandler<DeleteGitRepoCommand>
{
    private readonly IGitRepoRepository _gitRepoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGitRepoCommandHandler(IGitRepoRepository gitRepoRepository, IUnitOfWork unitOfWork)
    {
        _gitRepoRepository = gitRepoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteGitRepoCommand command, CancellationToken cancellationToken)
    {
        GitRepo? gitRepo = await _gitRepoRepository.GetByName(command.Key, cancellationToken);
        if (gitRepo is null)
        {
            return SupportToolsServerApiClientErrors.GitWithKeyNotFound(command.Key);
        }

        _gitRepoRepository.Delete(gitRepo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
