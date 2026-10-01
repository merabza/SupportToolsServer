using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//რეპოზიტორიის დამატების ან რედაქტირების შემდეგ მისი პროექტის განახლება სამუშაო ფოლდერში რიგში დგება
public sealed class GitRepoSavedDomainEventHandler : IDomainEventHandler<GitRepoAddedDomainEvent>,
    IDomainEventHandler<GitRepoUpdatedDomainEvent>
{
    private readonly IGitProjectUpdateQueue _gitProjectUpdateQueue;

    public GitRepoSavedDomainEventHandler(IGitProjectUpdateQueue gitProjectUpdateQueue)
    {
        _gitProjectUpdateQueue = gitProjectUpdateQueue;
    }

    public Task Handle(GitRepoAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Enqueue(domainEvent.Name, domainEvent.Address, domainEvent.FolderName, cancellationToken);
    }

    public Task Handle(GitRepoUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Enqueue(domainEvent.Name, domainEvent.Address, domainEvent.FolderName, cancellationToken);
    }

    private Task Enqueue(string name, string address, string folderName, CancellationToken cancellationToken)
    {
        return _gitProjectUpdateQueue.Enqueue(new UpdateGitProjectCommand(name, address, folderName), cancellationToken)
            .AsTask();
    }
}
