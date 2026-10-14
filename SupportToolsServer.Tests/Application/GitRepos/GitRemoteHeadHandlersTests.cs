using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitRepos.GetGitRemoteHeads;
using SupportToolsServer.Application.GitRepos.GitRemoteHeads;
using SupportToolsServer.Application.GitRepos.NotifyGitPushed;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class GitRemoteHeadHandlersTests
{
    private readonly Mock<IGitRemoteHeadsCache> _cache = new();
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly Mock<IGitProjectUpdateQueue> _queue = new();

    //The cache gets every git in name order without case and its result is returned as it is
    [Fact]
    public async Task GetGitRemoteHeads_AsksTheCacheForEveryGitInNameOrder()
    {
        GitRepo repoB = TestData.NewGitRepo("repoB", _cSharp);
        GitRepo repoA = TestData.NewGitRepo("RepoA", _cSharp);
        GitRepo repoC = TestData.NewGitRepo("RepoC", _cSharp);
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([repoC, repoB, repoA]);
        List<StsGitRemoteHeadDataModel> heads = [new() { GitName = "RepoA" }];
        IReadOnlyList<GitRepo>? asked = null;
        using var cancellation = new CancellationTokenSource();
        _cache.Setup(c => c.GetHeads(It.IsAny<IReadOnlyList<GitRepo>>(), cancellation.Token))
            .Callback((IReadOnlyList<GitRepo> gitRepos, CancellationToken _) => asked = gitRepos)
            .ReturnsAsync(heads);
        var handler = new GetGitRemoteHeadsQueryHandler(_gitRepos.Object, _cache.Object);

        Result<List<StsGitRemoteHeadDataModel>> result =
            await handler.Handle(new GetGitRemoteHeadsQuery([]), cancellation.Token);

        Assert.Same(heads, result.Value);
        Assert.Equal(["RepoA", "repoB", "RepoC"], asked!.Select(x => x.Name));
    }

    //Only the named gits are checked, so the sync of one project does not read every remote. The names match without
    //case; a name the server does not know is left out
    [Fact]
    public async Task GetGitRemoteHeads_AsksTheCacheOnlyForTheNamedGits()
    {
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewGitRepo("RepoC", _cSharp), TestData.NewGitRepo("RepoB", _cSharp),
            TestData.NewGitRepo("RepoA", _cSharp)
        ]);
        IReadOnlyList<GitRepo>? asked = null;
        _cache.Setup(c => c.GetHeads(It.IsAny<IReadOnlyList<GitRepo>>(), It.IsAny<CancellationToken>()))
            .Callback((IReadOnlyList<GitRepo> gitRepos, CancellationToken _) => asked = gitRepos)
            .ReturnsAsync([]);
        var handler = new GetGitRemoteHeadsQueryHandler(_gitRepos.Object, _cache.Object);

        await handler.Handle(new GetGitRemoteHeadsQuery(["repoc", "RepoA", "Unknown"]), CancellationToken.None);

        Assert.Equal(["RepoA", "RepoC"], asked!.Select(x => x.Name));
    }

    [Fact]
    public async Task NotifyGitPushed_Fails_WhenTheGitIsNotRegistered()
    {
        var handler = new NotifyGitPushedCommandHandler(_gitRepos.Object, _cache.Object, _queue.Object);

        Result result = await handler.Handle(new NotifyGitPushedCommand("RepoZ"), CancellationToken.None);

        Assert.Equal("GitWithKeyNotFound", result.Error.Code);
        _cache.VerifyNoOtherCalls();
        _queue.VerifyNoOtherCalls();
    }

    //The key of the route may differ in case: the cache and the queue get the stored git
    [Fact]
    public async Task NotifyGitPushed_InvalidatesTheCachedHeadAndQueuesTheUpdateOfTheClone()
    {
        GitRepo repoA = TestData.NewGitRepo("RepoA", _cSharp);
        _gitRepos.Setup(r => r.GetByName("repoa", It.IsAny<CancellationToken>())).ReturnsAsync(repoA);
        using var cancellation = new CancellationTokenSource();
        var handler = new NotifyGitPushedCommandHandler(_gitRepos.Object, _cache.Object, _queue.Object);

        Result result = await handler.Handle(new NotifyGitPushedCommand("repoa"), cancellation.Token);

        Assert.True(result.IsSuccess);
        _cache.Verify(c => c.Invalidate("RepoA", cancellation.Token), Times.Once);
        _queue.Verify(
            q => q.Enqueue(new UpdateGitProjectCommand(repoA.Id, "RepoA", repoA.Address, repoA.FolderName),
                cancellation.Token), Times.Once);
    }
}
