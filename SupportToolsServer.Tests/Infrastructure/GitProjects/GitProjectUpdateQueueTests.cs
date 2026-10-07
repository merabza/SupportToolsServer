using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServerCore.Domain.GitRepos;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.GitProjects;

public sealed class GitProjectUpdateQueueTests
{
    private readonly GitProjectUpdateQueue _sut = new();

    [Fact]
    public async Task ReadAll_ReturnsTheEnqueuedCommandsInOrder()
    {
        var first = new UpdateGitProjectCommand(GitRepoId.CreateUnique(), "RepoA", "addressA", "FolderA");
        var second = new UpdateGitProjectCommand(GitRepoId.CreateUnique(), "RepoB", "addressB", "FolderB");
        await _sut.Enqueue(first, CancellationToken.None);
        await _sut.Enqueue(second, CancellationToken.None);

        List<UpdateGitProjectCommand> read = [];
        using var cancellationTokenSource = new CancellationTokenSource();
        await foreach (UpdateGitProjectCommand command in _sut.ReadAll(cancellationTokenSource.Token))
        {
            read.Add(command);
            if (read.Count == 2)
            {
                break;
            }
        }

        Assert.Equal([first, second], read);
    }

    [Fact]
    public async Task ReadAll_WaitsUntilACommandIsEnqueued_AndStopsOnCancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        IAsyncEnumerator<UpdateGitProjectCommand> enumerator = _sut.ReadAll(cancellationTokenSource.Token)
            .GetAsyncEnumerator(cancellationTokenSource.Token);
        await using (enumerator)
        {
            ValueTask<bool> next = enumerator.MoveNextAsync();
            Assert.False(next.IsCompleted);

            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await next);
        }
    }

    [Fact]
    public async Task Enqueue_Throws_WhenCancelled()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _sut.Enqueue(new UpdateGitProjectCommand(GitRepoId.CreateUnique(), "RepoA", "addressA", "FolderA"),
                cancellationTokenSource.Token));
    }
}
