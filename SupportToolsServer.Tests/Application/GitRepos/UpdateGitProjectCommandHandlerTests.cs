using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class UpdateGitProjectCommandHandlerTests
{
    private const string Address = "git@github.com:test/RepoA.git";
    private const string ProjectPath = @"C:\Work\Gits\Sub.RepoA";

    private static readonly Error TestError = Error.Problem("TestError", "Test error");

    //Strict: every git command the handler runs must be set up by the test
    private readonly Mock<IGitClient> _gitClient = new(MockBehavior.Strict);
    private readonly Mock<IGitsWorkFolder> _gitsWorkFolder = new(MockBehavior.Strict);

    public UpdateGitProjectCommandHandlerTests()
    {
        _gitsWorkFolder.Setup(f => f.GetProjectFolderPath("Sub/RepoA")).Returns(ProjectPath);
    }

    private Task<Result> Handle()
    {
        var handler = new UpdateGitProjectCommandHandler(_gitsWorkFolder.Object, _gitClient.Object);
        return handler.Handle(new UpdateGitProjectCommand("RepoA", Address, "Sub/RepoA"), CancellationToken.None);
    }

    //The folder exists, was cloned from the same address and has no local changes
    private void GivenACleanClone()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns(Address);
        _gitClient.Setup(c => c.HasChanges(ProjectPath)).Returns(false);
    }

    [Fact]
    public async Task Handle_ReturnsTheError_WhenTheProjectFolderPathCannotBeCounted()
    {
        _gitsWorkFolder.Setup(f => f.GetProjectFolderPath("Sub/RepoA")).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public async Task Handle_ClonesTheProject_WhenTheFolderDoesNotExist()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(false);
        _gitClient.Setup(c => c.Clone(Address, ProjectPath)).Returns(Result.Success());

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        _gitClient.Verify(c => c.Clone(Address, ProjectPath), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsTheCloneError()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(false);
        _gitClient.Setup(c => c.Clone(Address, ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsTheError_WhenTheRemoteOriginUrlCannotBeRead()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public async Task Handle_DeletesAndReclonesTheFolder_WhenItWasClonedFromAnotherAddress()
    {
        _gitsWorkFolder.SetupSequence(f => f.Exists(ProjectPath)).Returns(true).Returns(false);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns("git@github.com:test/Other.git");
        _gitsWorkFolder.Setup(f => f.Delete(ProjectPath));
        _gitClient.Setup(c => c.Clone(Address, ProjectPath)).Returns(Result.Success());

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        _gitsWorkFolder.Verify(f => f.Delete(ProjectPath), Times.Once);
        _gitClient.Verify(c => c.Clone(Address, ProjectPath), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsTheError_WhenTheStatusCannotBeRead()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns(Address);
        _gitClient.Setup(c => c.HasChanges(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public async Task Handle_RestoresTheFolderWithoutPulling_WhenItHasLocalChanges()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns(Address);
        _gitClient.Setup(c => c.HasChanges(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.Restore(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        _gitClient.Verify(c => c.Restore(ProjectPath), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsTheRemoteUpdateError()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsTheError_WhenTheNeedForPullCannotBeCounted()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public async Task Handle_PullsAndReturnsThePullResult_WhenPullIsNeeded()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.Pull(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        _gitClient.Verify(c => c.Pull(ProjectPath), Times.Once);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTheProjectIsUpToDate()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(false);

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        _gitClient.Verify(c => c.RemoteUpdate(ProjectPath), Times.Once);
        _gitsWorkFolder.Verify(f => f.Delete(It.IsAny<string>()), Times.Never);
    }
}
