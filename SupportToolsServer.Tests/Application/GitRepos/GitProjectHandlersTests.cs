using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.GitRepos.GetGitProjects;
using SupportToolsServer.Application.GitRepos.RefreshGitProjects;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepoProjects;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class GitProjectHandlersTests
{
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
    private readonly Mock<IGitRepoProjectRepository> _gitRepoProjects = new();
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly Mock<IGitProjectUpdateQueue> _queue = new();

    private static GitRepoProject Project(GitRepo gitRepo, string relativePath, string fileName,
        params string[] dependsOnProjectNames)
    {
        return GitRepoProject.Create(gitRepo.Id, relativePath, fileName, dependsOnProjectNames);
    }

    //The client form: the name of the git, the relative path and the file name, ordered without case by the git, the
    //path and the file. The dependencies come in name order, whatever order the scan found them in
    [Fact]
    public async Task GetGitProjects_ReturnsEveryProjectInTheClientFormInGitPathAndFileOrder()
    {
        GitRepo repoB = TestData.NewGitRepo("repoB", _cSharp);
        GitRepo repoA = TestData.NewGitRepo("RepoA", _cSharp);
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([repoB, repoA]);
        _gitRepoProjects.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            Project(repoB, @"repoB\Front", "front.esproj"), Project(repoA, @"RepoA\lib", "Lib.csproj"),
            Project(repoA, @"RepoA\App", "App.csproj", "Lib", "Common", "base"),
            Project(repoA, @"RepoA\App", "App.Tests.csproj", "App"), Project(repoB, @"repoB\Front", "Front.csproj")
        ]);
        var handler = new GetGitProjectsQueryHandler(_gitRepoProjects.Object, _gitRepos.Object);

        Result<List<StsGitProjectDataModel>> result =
            await handler.Handle(new GetGitProjectsQuery(), CancellationToken.None);

        Assert.Equal([
            ("RepoA", @"RepoA\App", "App.csproj", "base,Common,Lib"),
            ("RepoA", @"RepoA\App", "App.Tests.csproj", "App"), ("RepoA", @"RepoA\lib", "Lib.csproj", ""),
            ("repoB", @"repoB\Front", "Front.csproj", ""), ("repoB", @"repoB\Front", "front.esproj", "")
        ], result.Value.Select(x => (x.GitName, x.ProjectRelativePath, x.ProjectFileName,
            string.Join(",", x.DependsOnProjectNames))));
    }

    //Both projects of a name that two repositories have are returned: the client chooses one
    [Fact]
    public async Task GetGitProjects_ReturnsAProjectNameOfTwoRepositoriesTwice()
    {
        GitRepo crawler = TestData.NewGitRepo("CrawlerConsole", _cSharp);
        GitRepo systemTools = TestData.NewGitRepo("SystemTools", _cSharp);
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([crawler, systemTools]);
        _gitRepoProjects.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            Project(systemTools, @"SystemTools\SystemTools.DependencyInjection",
                "SystemTools.DependencyInjection.csproj"),
            Project(crawler, @"CrawlerConsole\SystemTools.DependencyInjection",
                "SystemTools.DependencyInjection.csproj")
        ]);
        var handler = new GetGitProjectsQueryHandler(_gitRepoProjects.Object, _gitRepos.Object);

        Result<List<StsGitProjectDataModel>> result =
            await handler.Handle(new GetGitProjectsQuery(), CancellationToken.None);

        Assert.Equal(["CrawlerConsole", "SystemTools"], result.Value.Select(x => x.GitName));
    }

    //The repositories are read before their projects: the projects of a repository added in between are left out
    [Fact]
    public async Task GetGitProjects_LeavesOutTheProjectsOfAnUnknownRepository()
    {
        GitRepo repoA = TestData.NewGitRepo("RepoA", _cSharp);
        GitRepo added = TestData.NewGitRepo("Added", _cSharp);
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([repoA]);
        _gitRepoProjects.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            Project(added, @"Added\App", "App.csproj"), Project(repoA, @"RepoA\App", "App.csproj")
        ]);
        var handler = new GetGitProjectsQueryHandler(_gitRepoProjects.Object, _gitRepos.Object);

        Result<List<StsGitProjectDataModel>> result =
            await handler.Handle(new GetGitProjectsQuery(), CancellationToken.None);

        Assert.Equal("RepoA", Assert.Single(result.Value).GitName);
    }

    [Fact]
    public async Task GetGitProjects_ReturnsAnEmptyList_BeforeAnyScan()
    {
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewGitRepo("RepoA", _cSharp)]);
        _gitRepoProjects.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new GetGitProjectsQueryHandler(_gitRepoProjects.Object, _gitRepos.Object);

        Result<List<StsGitProjectDataModel>> result =
            await handler.Handle(new GetGitProjectsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    //Every git gets an update with its id, name, address and folder, in name order without case
    [Fact]
    public async Task RefreshGitProjects_EnqueuesTheUpdateOfEveryGitInNameOrder()
    {
        GitRepo repoC = TestData.NewGitRepo("RepoC", _cSharp);
        GitRepo repoB = TestData.NewGitRepo("repoB", _cSharp);
        GitRepo repoA = TestData.NewGitRepo("RepoA", _cSharp);
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([repoC, repoB, repoA]);
        List<UpdateGitProjectCommand> enqueued = [];
        _queue.Setup(q => q.Enqueue(It.IsAny<UpdateGitProjectCommand>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateGitProjectCommand, CancellationToken>((command, _) => enqueued.Add(command))
            .Returns(ValueTask.CompletedTask);
        using var cancellationTokenSource = new CancellationTokenSource();
        var handler = new RefreshGitProjectsCommandHandler(_gitRepos.Object, _queue.Object);

        Result result = await handler.Handle(new RefreshGitProjectsCommand(), cancellationTokenSource.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal([
            new UpdateGitProjectCommand(repoA.Id, "RepoA", TestData.AddressOf("RepoA"), "RepoA"),
            new UpdateGitProjectCommand(repoB.Id, "repoB", TestData.AddressOf("repoB"), "repoB"),
            new UpdateGitProjectCommand(repoC.Id, "RepoC", TestData.AddressOf("RepoC"), "RepoC")
        ], enqueued);
        _queue.Verify(q => q.Enqueue(It.IsAny<UpdateGitProjectCommand>(), cancellationTokenSource.Token),
            Times.Exactly(3));
        _gitRepos.Verify(r => r.GetAll(cancellationTokenSource.Token), Times.Once);
    }

    [Fact]
    public async Task RefreshGitProjects_EnqueuesNothing_WhenThereIsNoGit()
    {
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new RefreshGitProjectsCommandHandler(_gitRepos.Object, _queue.Object);

        Result result = await handler.Handle(new RefreshGitProjectsCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _queue.Verify(q => q.Enqueue(It.IsAny<UpdateGitProjectCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
