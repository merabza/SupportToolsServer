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

    [Fact]
    public async Task Handle_EnqueuesTheUpdateOfAnAddedGit()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        await _sut.Handle(new GitRepoAddedDomainEvent(GitRepoId.CreateUnique(), "RepoA", "addressA", "FolderA"),
            cancellationTokenSource.Token);

        _queue.Verify(
            q => q.Enqueue(new UpdateGitProjectCommand("RepoA", "addressA", "FolderA"), cancellationTokenSource.Token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EnqueuesTheUpdateOfAnUpdatedGit()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        await _sut.Handle(new GitRepoUpdatedDomainEvent(GitRepoId.CreateUnique(), "RepoB", "addressB", "FolderB"),
            cancellationTokenSource.Token);

        _queue.Verify(
            q => q.Enqueue(new UpdateGitProjectCommand("RepoB", "addressB", "FolderB"), cancellationTokenSource.Token),
            Times.Once);
    }
}
