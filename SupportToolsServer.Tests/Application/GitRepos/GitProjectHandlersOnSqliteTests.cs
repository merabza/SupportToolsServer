using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupportToolsServer.Application.GitRepos.GetGitProjects;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Infrastructure.Options;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerDbPart.Db;
using SystemTools.SharedKernel;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace SupportToolsServer.Tests.Application.GitRepos;

//The update of a git with the real work folder, scanner, repositories and unit of work on SQLite; only git is a mock,
//whose clone and pull write the project files. GET git/gitprojects then returns what the scans found
public sealed class GitProjectHandlersOnSqliteTests : IAsyncLifetime, IDisposable
{
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
    private readonly Mock<IGitClient> _gitClient = new();
    private readonly TempFolder _temp = new();
    private SupportToolsServerSqliteDatabase _database = null!;
    private GitRepo _repoA = null!;
    private GitRepo _repoB = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        _repoA = TestData.NewGitRepo("RepoA", _cSharp);
        _repoB = TestData.NewGitRepo("RepoB", _cSharp);
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.GitIgnoreFileTypes.Add(_cSharp);
        context.GitRepos.AddRange(_repoA, _repoB);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    public void Dispose()
    {
        _temp.Dispose();
    }

    private string ClonePath(GitRepo gitRepo)
    {
        return _temp.Combine("Work", "Gits", gitRepo.FolderName);
    }

    //The first update clones the git: the clone writes the given project files
    private void GivenTheCloneWrites(GitRepo gitRepo, params (string RelativePath, string Content)[] files)
    {
        string clonePath = ClonePath(gitRepo);
        _gitClient.Setup(c => c.Clone(gitRepo.Address, clonePath)).Returns(() =>
        {
            foreach ((string relativePath, string content) in files)
            {
                ProjectFiles.Write(clonePath, relativePath, content);
            }

            return Result.Success();
        });
    }

    private async Task<Result> Update(GitRepo gitRepo)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateGitProjectCommandHandler(
            new GitsWorkFolder(MsOptions.Create(new AppOptions { WorkFolder = _temp.Combine("Work") }),
                NullLogger<GitsWorkFolder>.Instance), _gitClient.Object, new GitProjectFilesScanner(),
            new GitRepoProjectRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(
            new UpdateGitProjectCommand(gitRepo.Id, gitRepo.Name, gitRepo.Address, gitRepo.FolderName),
            CancellationToken.None);
    }

    private async Task<List<string>> GitProjects()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler =
            new GetGitProjectsQueryHandler(new GitRepoProjectRepository(context), new GitRepoRepository(context));
        Result<List<StsGitProjectDataModel>> result =
            await handler.Handle(new GetGitProjectsQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        return
        [
            .. result.Value.Select(x =>
                $"{x.GitName}:{x.ProjectRelativePath}\\{x.ProjectFileName}:{string.Join(",", x.DependsOnProjectNames)}")
        ];
    }

    [Fact]
    public async Task Update_StoresTheProjectsOfTheClone_AndTheListReturnsThemInTheClientForm()
    {
        GivenTheCloneWrites(_repoA,
            (Path.Combine("AppA", "AppA.csproj"),
                ProjectFiles.Project(@"..\LibA\LibA.csproj", @"..\..\RepoB\LibB\LibB.csproj")),
            (Path.Combine("LibA", "LibA.csproj"), ProjectFiles.Project()));
        GivenTheCloneWrites(_repoB, (Path.Combine("LibB", "LibB.csproj"), ProjectFiles.Project()));

        Assert.True((await Update(_repoA)).IsSuccess);
        Assert.True((await Update(_repoB)).IsSuccess);

        Assert.Equal([
            @"RepoA:RepoA\AppA\AppA.csproj:LibA,LibB", @"RepoA:RepoA\LibA\LibA.csproj:",
            @"RepoB:RepoB\LibB\LibB.csproj:"
        ], await GitProjects());
    }

    //The next update scans the pulled clone again: its projects replace the stored ones of that git only, also when a
    //project file is found again
    [Fact]
    public async Task Update_ReplacesTheStoredProjectsOfTheGitWithTheProjectsOfThePulledClone()
    {
        GivenTheCloneWrites(_repoA, (Path.Combine("AppA", "AppA.csproj"), ProjectFiles.Project(@"..\LibA\LibA.csproj")),
            (Path.Combine("LibA", "LibA.csproj"), ProjectFiles.Project()));
        GivenTheCloneWrites(_repoB, (Path.Combine("LibB", "LibB.csproj"), ProjectFiles.Project()));
        await Update(_repoA);
        await Update(_repoB);
        string clonePathA = ClonePath(_repoA);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(clonePathA)).Returns(_repoA.Address);
        _gitClient.Setup(c => c.HasChanges(clonePathA)).Returns(false);
        _gitClient.Setup(c => c.RemoteUpdate(clonePathA)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(clonePathA)).Returns(true);
        _gitClient.Setup(c => c.Pull(clonePathA)).Returns(() =>
        {
            Directory.Delete(Path.Combine(clonePathA, "LibA"), true);
            ProjectFiles.Write(clonePathA, Path.Combine("AppA", "AppA.csproj"),
                ProjectFiles.Project(@"..\Libs\LibC\LibC.csproj"));
            ProjectFiles.Write(clonePathA, Path.Combine("Libs", "LibC", "LibC.csproj"), ProjectFiles.Project());
            return Result.Success();
        });

        Result result = await Update(_repoA);

        Assert.True(result.IsSuccess);
        Assert.Equal([
            @"RepoA:RepoA\AppA\AppA.csproj:LibC", @"RepoA:RepoA\Libs\LibC\LibC.csproj:",
            @"RepoB:RepoB\LibB\LibB.csproj:"
        ], await GitProjects());
    }

    //A project file that cannot be read fails the scan: the projects stored before stay
    [Fact]
    public async Task Update_KeepsTheStoredProjects_WhenTheScanFails()
    {
        GivenTheCloneWrites(_repoA, (Path.Combine("AppA", "AppA.csproj"), ProjectFiles.Project()));
        await Update(_repoA);
        string clonePathA = ClonePath(_repoA);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(clonePathA)).Returns(_repoA.Address);
        _gitClient.Setup(c => c.HasChanges(clonePathA)).Returns(false);
        _gitClient.Setup(c => c.RemoteUpdate(clonePathA)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(clonePathA)).Returns(true);
        _gitClient.Setup(c => c.Pull(clonePathA)).Returns(() =>
        {
            ProjectFiles.Write(clonePathA, Path.Combine("Broken", "Broken.csproj"), "<Project>");
            return Result.Success();
        });

        Result result = await Update(_repoA);

        Assert.Equal(nameof(GitProjectsErrors.ProjectFileCannotBeRead), result.Error.Code);
        Assert.Equal([@"RepoA:RepoA\AppA\AppA.csproj:"], await GitProjects());
    }
}
