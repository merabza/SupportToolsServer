using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitRepos.DeleteGitRepo;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.Projects;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class DeleteGitRepoCommandHandlerTests
{
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly List<Project> _projectList = [];
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly GitRepo _stored = TestData.NewGitRepo("RepoA", TestData.NewGitIgnoreFileType("CSharp"));
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public DeleteGitRepoCommandHandlerTests()
    {
        _projects.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => _projectList);
    }

    private Task<Result> Handle(string key)
    {
        var handler = new DeleteGitRepoCommandHandler(_gitRepos.Object, _projects.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteGitRepoCommand(key), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ReturnsGitWithKeyNotFound_WhenThereIsNoSuchGit()
    {
        _gitRepos.Setup(r => r.GetByName("RepoA", It.IsAny<CancellationToken>())).ReturnsAsync((GitRepo?)null);

        Result result = await Handle("RepoA");

        Assert.Equal("GitWithKeyNotFound", result.Error.Code);
        _gitRepos.Verify(r => r.Delete(It.IsAny<GitRepo>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    //A project that uses another git does not use this one
    [Fact]
    public async Task Handle_DeletesTheGitAndSaves()
    {
        _gitRepos.Setup(r => r.GetByName("RepoA", It.IsAny<CancellationToken>())).ReturnsAsync(_stored);
        _projectList.Add(TestData.NewProject("AppA",
            gitRepos: [TestData.NewGitRepo("RepoB", TestData.NewGitIgnoreFileType("CSharp"))]));

        Result result = await Handle("RepoA");

        Assert.True(result.IsSuccess);
        _gitRepos.Verify(r => r.Delete(_stored), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The projects that use the git, as a project git or as a scaffold seeder git, in name order and each once
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheProjects_WhenProjectsUseTheGit()
    {
        _gitRepos.Setup(r => r.GetByName("RepoA", It.IsAny<CancellationToken>())).ReturnsAsync(_stored);
        _projectList.AddRange(TestData.NewProject("Zeta", scaffoldSeederGitRepos: [_stored]),
            TestData.NewProject("AppB", gitRepos: [_stored], scaffoldSeederGitRepos: [_stored]),
            TestData.NewProject("AppC"));

        Result result = await Handle("RepoA");

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("GitRepo RepoA Is Used By: Project AppB, Project Zeta", result.Error.Description);
        _gitRepos.Verify(r => r.Delete(It.IsAny<GitRepo>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
