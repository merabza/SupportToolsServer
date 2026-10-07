using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepoProjects;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class GitRepoProjectRepositoryTests : IAsyncLifetime
{
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
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
        context.GitRepoProjects.AddRange(Project(_repoA, "AppA", "LibA", "LibB"), Project(_repoA, "LibA"),
            Project(_repoB, "AppB", "LibA"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    //A project in the folder of its name, as a csproj in a repository usually is
    private static GitRepoProject Project(GitRepo gitRepo, string name, params string[] dependsOnProjectNames)
    {
        return GitRepoProject.Create(gitRepo.Id, $@"{gitRepo.Name}\{name}", $"{name}.csproj", dependsOnProjectNames);
    }

    private static string Describe(GitRepoProject gitRepoProject)
    {
        return $"{gitRepoProject.ProjectRelativePath}\\{gitRepoProject.ProjectFileName}:" +
               string.Join(",", gitRepoProject.Dependencies.Select(x => x.ProjectName).Order());
    }

    //Every stored project with its dependencies, ordered
    private async Task<List<string>> StoredProjects()
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        List<GitRepoProject> projects =
            await check.GitRepoProjects.AsNoTracking().Include(x => x.Dependencies).ToListAsync();
        return [.. projects.Select(Describe).Order()];
    }

    //Every dependency row of the database, so that orphans would show
    private async Task<List<string>> StoredDependencyRows()
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.Set<GitRepoProjectDependency>().AsNoTracking().Select(x => x.ProjectName).Order()
            .ToListAsync();
    }

    [Fact]
    public async Task GetAll_ReturnsEveryProjectWithItsDependenciesWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<GitRepoProject> all = await new GitRepoProjectRepository(context).GetAll(CancellationToken.None);

        Assert.Equal([@"RepoA\AppA\AppA.csproj:LibA,LibB", @"RepoA\LibA\LibA.csproj:", @"RepoB\AppB\AppB.csproj:LibA"],
            all.Select(Describe).Order());
        Assert.Equal([_repoA.Id, _repoA.Id, _repoB.Id],
            all.OrderBy(x => x.ProjectRelativePath).Select(x => x.GitRepoId));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //The projects of the repository are replaced and nothing of them is left, while the other repository keeps its
    //projects. A project file found again (AppA) gets a new row: the old one goes before it is inserted
    [Fact]
    public async Task Replace_ReplacesTheProjectsOfTheRepositoryOnly_AndLeavesNoOrphan()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            await new GitRepoProjectRepository(context).Replace(_repoA.Id,
                [Project(_repoA, "AppA", "LibC"), Project(_repoA, "LibC", "LibD")], CancellationToken.None);
            await context.SaveChangesAsync();
        }

        Assert.Equal([@"RepoA\AppA\AppA.csproj:LibC", @"RepoA\LibC\LibC.csproj:LibD", @"RepoB\AppB\AppB.csproj:LibA"],
            await StoredProjects());
        Assert.Equal(["LibA", "LibC", "LibD"], await StoredDependencyRows());
    }

    [Fact]
    public async Task Replace_RemovesEveryProjectOfTheRepository_WhenTheScanFoundNone()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            await new GitRepoProjectRepository(context).Replace(_repoA.Id, [], CancellationToken.None);
            await context.SaveChangesAsync();
        }

        Assert.Equal([@"RepoB\AppB\AppB.csproj:LibA"], await StoredProjects());
        Assert.Equal(["LibA"], await StoredDependencyRows());
    }

    //Nothing is written before the save: the replacement is one transaction of the handler
    [Fact]
    public async Task Replace_WritesNothingWithoutSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            await new GitRepoProjectRepository(context).Replace(_repoA.Id, [], CancellationToken.None);
        }

        Assert.Equal(3, (await StoredProjects()).Count);
    }

    [Fact]
    public async Task Replace_IsRefusedOnSave_WhenAProjectFileRepeatsInTheRepository()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        await new GitRepoProjectRepository(context).Replace(_repoA.Id,
            [Project(_repoA, "AppA"), Project(_repoA, "AppA", "LibA")], CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The same project file may be stored for two repositories (the relative paths start with the folder of the clone,
    //so this happens only for a test like this one)
    [Fact]
    public async Task Replace_StoresTheSameProjectFileForAnotherRepository()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            await new GitRepoProjectRepository(context).Replace(_repoB.Id,
                [GitRepoProject.Create(_repoB.Id, @"RepoA\AppA", "AppA.csproj", [])], CancellationToken.None);
            await context.SaveChangesAsync();
        }

        Assert.Equal(2,
            (await StoredProjects()).Count(x => x.StartsWith(@"RepoA\AppA\AppA.csproj:", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenADependencyRepeatsInTheProject()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        await new GitRepoProjectRepository(context).Replace(_repoA.Id, [Project(_repoA, "AppA", "LibA", "LibA")],
            CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The scan of a repository deleted meanwhile fails on its foreign key
    [Fact]
    public async Task Replace_IsRefusedOnSave_WhenTheRepositoryDoesNotExist()
    {
        var missing = GitRepoId.CreateUnique();
        await using SupportToolsServerDbContext context = _database.NewContext();
        await new GitRepoProjectRepository(context).Replace(missing,
            [GitRepoProject.Create(missing, @"RepoZ\AppZ", "AppZ.csproj", [])], CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The projects are the result of a scan of the repository, so they go with it
    [Fact]
    public async Task DeletingTheRepository_DeletesItsProjectsAndTheirDependencies()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            var gitRepoRepository = new GitRepoRepository(context);
            gitRepoRepository.Delete((await gitRepoRepository.GetByName("RepoA", CancellationToken.None))!);
            await context.SaveChangesAsync();
        }

        Assert.Equal([@"RepoB\AppB\AppB.csproj:LibA"], await StoredProjects());
        Assert.Equal(["LibA"], await StoredDependencyRows());
    }
}
