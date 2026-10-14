using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SupportToolsServer.Application.GitRepos.GitRemoteHeads;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Infrastructure.Options;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.SharedKernel;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace SupportToolsServer.Tests.Infrastructure.GitProjects;

public sealed class GitRemoteHeadsCacheTests : IDisposable
{
    private const int LifetimeMinutes = 10;

    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
    private readonly Mock<IGitClient> _gitClient = new();
    private readonly CollectingLogger<GitRemoteHeadsCache> _logger = new();
    private readonly GitRepo _repoA;
    private readonly GitRepo _repoB;
    private readonly GitRemoteHeadsCache _sut;
    private readonly ManualTimeProvider _timeProvider = new();

    public GitRemoteHeadsCacheTests()
    {
        _repoA = TestData.NewGitRepo("RepoA", _cSharp);
        _repoB = TestData.NewGitRepo("RepoB", _cSharp);
        _gitClient.Setup(c => c.GetRemoteHead(It.IsAny<string>()))
            .Returns((string address) => new GitRemoteHead("main", $"commit of {address}"));
        _sut = CreateCache(LifetimeMinutes);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    private GitRemoteHeadsCache CreateCache(int lifetimeMinutes)
    {
        return new GitRemoteHeadsCache(_gitClient.Object,
            MsOptions.Create(new AppOptions { WorkFolder = "Work", GitRemoteHeadsCacheMinutes = lifetimeMinutes }),
            _timeProvider, _logger);
    }

    private Task<List<StsGitRemoteHeadDataModel>> GetHeads(params GitRepo[] gitRepos)
    {
        return _sut.GetHeads(gitRepos, CancellationToken.None);
    }

    private void VerifyReads(GitRepo gitRepo, int times)
    {
        _gitClient.Verify(c => c.GetRemoteHead(gitRepo.Address), Times.Exactly(times));
    }

    [Fact]
    public async Task GetHeads_ReadsEveryRemoteAndReturnsTheHeadsInTheGivenOrder()
    {
        List<StsGitRemoteHeadDataModel> heads = await GetHeads(_repoB, _repoA);

        Assert.Equal([
            ("RepoB", "main", $"commit of {_repoB.Address}", null),
            ("RepoA", "main", $"commit of {_repoA.Address}", null)
        ], heads.Select(x => (x.GitName, x.BranchName, x.CommitId, x.ErrorDescription)));
        VerifyReads(_repoA, 1);
        VerifyReads(_repoB, 1);
    }

    [Fact]
    public async Task GetHeads_TakesAHeadFromTheCache_BeforeItsLifetimeEnds()
    {
        await GetHeads(_repoA);
        _timeProvider.Advance(TimeSpan.FromMinutes(LifetimeMinutes) - TimeSpan.FromSeconds(1));

        StsGitRemoteHeadDataModel head = Assert.Single(await GetHeads(_repoA));

        Assert.Equal($"commit of {_repoA.Address}", head.CommitId);
        VerifyReads(_repoA, 1);
    }

    [Fact]
    public async Task GetHeads_ReadsTheRemoteAgain_WhenTheLifetimeEnds()
    {
        await GetHeads(_repoA);
        _gitClient.Setup(c => c.GetRemoteHead(_repoA.Address)).Returns(new GitRemoteHead("main", "new commit"));
        _timeProvider.Advance(TimeSpan.FromMinutes(LifetimeMinutes));

        StsGitRemoteHeadDataModel head = Assert.Single(await GetHeads(_repoA));

        Assert.Equal("new commit", head.CommitId);
        VerifyReads(_repoA, 2);
    }

    //Only the expired heads are read again
    [Fact]
    public async Task GetHeads_ReadsOnlyTheHeadsThatAreNotInTheCache()
    {
        await GetHeads(_repoA);

        await GetHeads(_repoA, _repoB);

        VerifyReads(_repoA, 1);
        VerifyReads(_repoB, 1);
    }

    [Fact]
    public async Task GetHeads_ReadsTheRemoteAgain_WhenTheAddressOfTheGitChanged()
    {
        await GetHeads(_repoA);
        GitRepo moved = TestData.NewGitRepo("RepoA", _cSharp, "git@github.com:test/moved.git");

        StsGitRemoteHeadDataModel head = Assert.Single(await GetHeads(moved));

        Assert.Equal("commit of git@github.com:test/moved.git", head.CommitId);
    }

    //The names of the gits match without case, like the unique index
    [Fact]
    public async Task GetHeads_FindsTheCachedHeadByTheNameWithoutCase()
    {
        await GetHeads(_repoA);
        GitRepo lowerCase = TestData.NewGitRepo("repoa", _cSharp, _repoA.Address);

        StsGitRemoteHeadDataModel head = Assert.Single(await GetHeads(lowerCase));

        Assert.Equal("repoa", head.GitName);
        VerifyReads(_repoA, 1);
    }

    [Fact]
    public async Task GetHeads_ReadsEveryTime_WhenTheLifetimeIsZero()
    {
        using GitRemoteHeadsCache sut = CreateCache(0);

        await sut.GetHeads([_repoA], CancellationToken.None);
        await sut.GetHeads([_repoA], CancellationToken.None);

        VerifyReads(_repoA, 2);
    }

    //The error of git holds the command line with the address, which may hold a token: the client gets a general
    //description, the log only the code. A failure is not cached, the next request tries again
    [Fact]
    public async Task GetHeads_ReturnsAGeneralErrorAndDoesNotCacheIt_WhenTheRemoteCannotBeRead()
    {
        _gitClient.Setup(c => c.GetRemoteHead(_repoA.Address)).Returns(Result.Failure<GitRemoteHead>(
            Error.Problem("RunProcessError", $"git ls-remote --symref -- {_repoA.Address} HEAD failed")));

        StsGitRemoteHeadDataModel head = Assert.Single(await GetHeads(_repoA));
        await GetHeads(_repoA);

        Assert.Equal(("RepoA", null, null, "Remote head of git RepoA cannot be read"),
            (head.GitName, head.BranchName, head.CommitId, head.ErrorDescription));
        VerifyReads(_repoA, 2);
        Assert.All(_logger.Entries, e => Assert.DoesNotContain(_repoA.Address, e.Message, StringComparison.Ordinal));
        Assert.Contains(_logger.Entries,
            e => e is { Level: LogLevel.Warning, Message: "Remote head of git RepoA cannot be read: RunProcessError" });
    }

    //A failure drops the head that was cached before, so an old commit is never returned after it
    [Fact]
    public async Task GetHeads_DropsTheCachedHead_WhenTheRemoteCannotBeReadAgain()
    {
        await GetHeads(_repoA);
        _timeProvider.Advance(TimeSpan.FromMinutes(LifetimeMinutes));
        _gitClient.Setup(c => c.GetRemoteHead(_repoA.Address))
            .Returns(Result.Failure<GitRemoteHead>(GitProjectsErrors.RemoteHeadIsNotFound));
        await GetHeads(_repoA);
        _gitClient.Setup(c => c.GetRemoteHead(_repoA.Address)).Returns(new GitRemoteHead("main", "new commit"));

        StsGitRemoteHeadDataModel head = Assert.Single(await GetHeads(_repoA));

        Assert.Equal("new commit", head.CommitId);
    }

    [Fact]
    public async Task Invalidate_MakesTheNextRequestReadTheRemoteAgain()
    {
        await GetHeads(_repoA, _repoB);

        await _sut.Invalidate("repoA", CancellationToken.None);
        await GetHeads(_repoA, _repoB);

        VerifyReads(_repoA, 2);
        VerifyReads(_repoB, 1);
    }

    //Invalidate waits for the running refresh, so the head read before a push cannot stay in the cache after it
    [Fact]
    public async Task Invalidate_WaitsForTheRunningRefresh()
    {
        using var readStarted = new ManualResetEventSlim();
        using var releaseRead = new ManualResetEventSlim();
        _gitClient.Setup(c => c.GetRemoteHead(_repoA.Address)).Returns(() =>
        {
            readStarted.Set();
            releaseRead.Wait(TimeSpan.FromSeconds(10));
            return new GitRemoteHead("main", "commit before the push");
        });
        Task<List<StsGitRemoteHeadDataModel>> refresh = Task.Run(() => GetHeads(_repoA));
        Assert.True(readStarted.Wait(TimeSpan.FromSeconds(10)));

        Task invalidate = _sut.Invalidate("RepoA", CancellationToken.None);
        Assert.False(invalidate.IsCompleted);
        releaseRead.Set();
        await refresh;
        await invalidate;
        _gitClient.Setup(c => c.GetRemoteHead(_repoA.Address)).Returns(new GitRemoteHead("main", "pushed commit"));

        Assert.Equal("pushed commit", Assert.Single(await GetHeads(_repoA)).CommitId);
    }

    //Dispose releases the lock of the refresh, so a disposed cache is not used again
    [Fact]
    public async Task Dispose_ReleasesTheLockOfTheRefresh()
    {
        // Arrange
        GitRemoteHeadsCache sut = CreateCache(LifetimeMinutes);

        // Act
        sut.Dispose();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.GetHeads([_repoA], CancellationToken.None));
    }

    //At most MaxParallelism remotes are read at the same time
    [Fact]
    public async Task GetHeads_ReadsAtMostEightRemotesAtTheSameTime()
    {
        int running = 0;
        int maxRunning = 0;
        _gitClient.Setup(c => c.GetRemoteHead(It.IsAny<string>())).Returns((string address) =>
        {
            int now = Interlocked.Increment(ref running);
            InterlockedMax(ref maxRunning, now);
            //The time of a call to git, so that the reads overlap
            SpinWait.SpinUntil(() => false, 20);
            Interlocked.Decrement(ref running);
            return new GitRemoteHead("main", $"commit of {address}");
        });
        GitRepo[] gitRepos = [.. Enumerable.Range(0, 30).Select(i => TestData.NewGitRepo($"Repo{i}", _cSharp))];

        List<StsGitRemoteHeadDataModel> heads = await GetHeads(gitRepos);

        Assert.Equal(gitRepos.Select(x => x.Name), heads.Select(x => x.GitName));
        Assert.InRange(maxRunning, 1, GitRemoteHeadsCache.MaxParallelism);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current = Volatile.Read(ref target);
        while (value > current)
        {
            int previous = Interlocked.CompareExchange(ref target, value, current);
            if (previous == current)
            {
                return;
            }

            current = previous;
        }
    }
}
