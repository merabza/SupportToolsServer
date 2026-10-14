using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.GitRepos.GitRemoteHeads;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.NotifyGitPushed;

//push-ის შემდეგ რეპოზიტორიის ქეშირებული ვერსია უქმდება (კლიენტის ნათქვამს სერვერი ბრმად არ ენდობა: მომდევნო GET
//ვერსიას git ls-remote-ით გადაამოწმებს) და სერვერის კლონის განახლება და პროექტების სკანირება რიგში დგება (B9)
public sealed class NotifyGitPushedCommandHandler : ICommandHandler<NotifyGitPushedCommand>
{
    private readonly IGitProjectUpdateQueue _gitProjectUpdateQueue;
    private readonly IGitRemoteHeadsCache _gitRemoteHeadsCache;
    private readonly IGitRepoRepository _gitRepoRepository;

    public NotifyGitPushedCommandHandler(IGitRepoRepository gitRepoRepository,
        IGitRemoteHeadsCache gitRemoteHeadsCache, IGitProjectUpdateQueue gitProjectUpdateQueue)
    {
        _gitRepoRepository = gitRepoRepository;
        _gitRemoteHeadsCache = gitRemoteHeadsCache;
        _gitProjectUpdateQueue = gitProjectUpdateQueue;
    }

    public async Task<Result> Handle(NotifyGitPushedCommand command, CancellationToken cancellationToken)
    {
        GitRepo? gitRepo = await _gitRepoRepository.GetByName(command.Key, cancellationToken);
        if (gitRepo is null)
        {
            return SupportToolsServerApiClientErrors.GitWithKeyNotFound(command.Key);
        }

        await _gitRemoteHeadsCache.Invalidate(gitRepo.Name, cancellationToken);
        await _gitProjectUpdateQueue.Enqueue(
            new UpdateGitProjectCommand(gitRepo.Id, gitRepo.Name, gitRepo.Address, gitRepo.FolderName),
            cancellationToken);
        return Result.Success();
    }
}
