using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitRepos.DeleteGitRepo;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class DeleteGitRepoCommandHandlerTests
{
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_ReturnsGitWithKeyNotFound_WhenThereIsNoSuchGit()
    {
        _gitRepos.Setup(r => r.GetByName("RepoA", It.IsAny<CancellationToken>())).ReturnsAsync((GitRepo?)null);
        var handler = new DeleteGitRepoCommandHandler(_gitRepos.Object, _unitOfWork.Object);

        Result result = await handler.Handle(new DeleteGitRepoCommand("RepoA"), CancellationToken.None);

        Assert.Equal("GitWithKeyNotFound", result.Error.Code);
        _gitRepos.Verify(r => r.Delete(It.IsAny<GitRepo>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeletesTheGitAndSaves()
    {
        GitRepo stored = TestData.NewGitRepo("RepoA", TestData.NewGitIgnoreFileType("CSharp"));
        _gitRepos.Setup(r => r.GetByName("RepoA", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        var handler = new DeleteGitRepoCommandHandler(_gitRepos.Object, _unitOfWork.Object);

        Result result = await handler.Handle(new DeleteGitRepoCommand("RepoA"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gitRepos.Verify(r => r.Delete(stored), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
