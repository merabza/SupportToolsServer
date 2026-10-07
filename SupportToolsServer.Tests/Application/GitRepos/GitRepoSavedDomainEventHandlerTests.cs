using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServerCore.Domain.GitRepos;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class GitRepoSavedDomainEventHandlerTests
{
    private readonly Mock<IGitProjectUpdateQueue> _queue = new();
    private readonly GitRepoSavedDomainEventHandler _sut;

    public GitRepoSavedDomainEventHandlerTests()
    {
        _queue.Setup(q => q.Enqueue(It.IsAny<UpdateGitProjectCommand>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        _sut = new GitRepoSavedDomainEventHandler(_queue.Object);
    }

    //The id of the git goes with the command: the projects found by the scan are stored for it
    [Fact]
    public async Task Handle_EnqueuesTheUpdateOfAnAddedGit()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var gitRepoId = GitRepoId.CreateUnique();

        await _sut.Handle(new GitRepoAddedDomainEvent(gitRepoId, "RepoA", "addressA", "FolderA"),
            cancellationTokenSource.Token);

        _queue.Verify(
            q => q.Enqueue(new UpdateGitProjectCommand(new GitRepoId(gitRepoId.Value), "RepoA", "addressA", "FolderA"),
                cancellationTokenSource.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_EnqueuesTheUpdateOfAnUpdatedGit()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var gitRepoId = GitRepoId.CreateUnique();

        await _sut.Handle(new GitRepoUpdatedDomainEvent(gitRepoId, "RepoB", "addressB", "FolderB"),
            cancellationTokenSource.Token);

        _queue.Verify(
            q => q.Enqueue(new UpdateGitProjectCommand(new GitRepoId(gitRepoId.Value), "RepoB", "addressB", "FolderB"),
                cancellationTokenSource.Token), Times.Once);
    }
}
