using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.SmartSchemas;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

//An in-memory SQLite database with Foreign Keys=True: the references of the projects are real rows
public sealed class ProjectRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseServerConnection _connection = TestData.NewDatabaseServerConnection("Pc1.Sql");
    private readonly EditorConfigFileType _editorConfig = TestData.NewEditorConfigFileType("default");
    private readonly FileStorage _fileStorage = TestData.NewFileStorage("Backups");
    private readonly GitIgnoreFileType _gitIgnoreFileType = TestData.NewGitIgnoreFileType("CSharp");
    private readonly NpmPackage _react = TestData.NewNpmPackage("react");
    private readonly SmartSchema _smartSchema = TestData.NewSmartSchema("Reduce");
    private Project _appA = null!;
    private SupportToolsServerSqliteDatabase _database = null!;
    private GitRepo _repoA = null!;
    private GitRepo _repoB = null!;

    public async Task InitializeAsync()
    {
        _repoA = TestData.NewGitRepo("RepoA", _gitIgnoreFileType);
        _repoB = TestData.NewGitRepo("RepoB", _gitIgnoreFileType);
        _appA = TestData.NewProject("AppA", _editorConfig, TestData.NewDatabaseParameters(_connection),
            TestData.NewDatabaseParameters(_connection, _smartSchema, _fileStorage, "AppAProdCopy"), [_repoA],
            [_repoB], [_react]);
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.EditorConfigFileTypes.Add(_editorConfig);
        context.GitIgnoreFileTypes.Add(_gitIgnoreFileType);
        context.GitRepos.AddRange(_repoA, _repoB);
        context.NpmPackages.Add(_react);
        context.DatabaseServerConnections.Add(_connection);
        context.SmartSchemas.Add(_smartSchema);
        context.FileStorages.Add(_fileStorage);
        context.Projects.AddRange(_appA, TestData.NewProject("AppB"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<Project> Read(SupportToolsServerDbContext context, string name)
    {
        Project? project = await new ProjectRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<Project>(project);
    }

    private static async Task<Project> ReadForUpdate(SupportToolsServerDbContext context, string name)
    {
        Project? project = await new ProjectRepository(context).GetByNameForUpdate(name, CancellationToken.None);
        return Assert.IsType<Project>(project);
    }

    private async Task<Project?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await new ProjectRepository(check).GetByName(name, CancellationToken.None);
    }

    //Every child row of the database, so that orphans would show
    private async Task<List<string>> StoredChildRows()
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        List<string> rows =
        [
            .. await check.Set<ProjectGitRepo>().AsNoTracking().Select(x => "git " + x.Kind).ToListAsync(),
            .. await check.Set<ProjectNpmPackage>().AsNoTracking().Select(_ => "npm").ToListAsync(),
            .. await check.Set<ProjectRedundantFile>().AsNoTracking().Select(x => "file " + x.FileName).ToListAsync(),
            .. await check.Set<ProjectAllowedTool>().AsNoTracking().Select(x => "tool " + x.ToolName).ToListAsync(),
            .. await check.Set<ProjectEndpoint>().AsNoTracking().Select(x => "endpoint " + x.Name).ToListAsync(),
            .. await check.Set<ProjectRouteClass>().AsNoTracking().Select(x => "route " + x.Name).ToListAsync()
        ];
        return [.. rows.Order(StringComparer.Ordinal)];
    }

    private static void Update(Project project, DatabaseParameters? dev, DatabaseParameters? prodCopy,
        IEnumerable<ProjectGitRepo> gitRepos, IEnumerable<ProjectNpmPackage> npmPackages,
        IEnumerable<ProjectRedundantFile> redundantFiles)
    {
        project.Update(project.Name, project.ProjectType, null, null, 1, 0, false, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, dev, prodCopy, gitRepos, npmPackages, redundantFiles,
            [ProjectAllowedTool.Create("SeedData")], [], [ProjectRouteClass.Create("Main", "api", "v2", "/main")]);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryProjectWithItsDatabaseParametersAndChildrenWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<Project> all = await new ProjectRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["AppA", "AppB"], all.Select(x => x.Name).Order());
        Project appA = all.Single(x => x.Name == "AppA");
        Assert.Equal(_editorConfig.Id, appA.EditorConfigFileTypeId);
        Assert.Equal(_connection.Id, appA.DevDatabaseParameters!.DbConnectionId);
        Assert.Equal(_fileStorage.Id, appA.ProdCopyDatabaseParameters!.FileStorageId);
        Assert.Equal(2, appA.GitRepos.Count);
        Assert.Equal([_react.Id], appA.NpmPackages.Select(x => x.NpmPackageId));
        Assert.Single(appA.RedundantFiles);
        Assert.Single(appA.AllowedTools);
        Assert.Single(appA.Endpoints);
        Assert.Single(appA.RouteClasses);
        Assert.Null(all.Single(x => x.Name == "AppB").DevDatabaseParameters);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("AppA")]
    [InlineData("appa")]
    [InlineData("APPA")]
    public async Task GetByName_FindsTheNameWithoutCaseWithEveryPartAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Project found = await Read(context, name);

        Assert.Equal(_appA.Id, found.Id);
        Assert.Equal("AppA", found.Name);
        Assert.Equal(TestData.MadeUpKeyGuidPart, found.KeyGuidPart);
        Assert.Equal(@"D:\1WorkDotnet\AppA\AppA.slnx", found.SolutionFileName);
        DatabaseParameters prodCopy = Assert.IsType<DatabaseParameters>(found.ProdCopyDatabaseParameters);
        Assert.Equal(_connection.Id, prodCopy.DbConnectionId);
        Assert.Equal(_smartSchema.Id, prodCopy.SmartSchemaId);
        Assert.Equal(_fileStorage.Id, prodCopy.FileStorageId);
        Assert.Equal("AppAProdCopy", prodCopy.DatabaseName);
        Assert.Equal(120, prodCopy.CommandTimeOut);
        Assert.True(prodCopy.Compress);
        Assert.Null(prodCopy.Verify);
        Assert.Equal(
            [(_repoA.Id, EProjectGitRepoKind.Main), (_repoB.Id, EProjectGitRepoKind.ScaffoldSeed)],
            found.GitRepos.Select(x => (x.GitRepoId, x.Kind)).OrderBy(x => x.Kind));
        Assert.Equal("/upload", Assert.Single(found.Endpoints).EndpointRoute);
        Assert.Equal("v1", Assert.Single(found.RouteClasses).ApiVersion);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new ProjectRepository(context).GetByName("AppZ", CancellationToken.None));
    }

    //Only the project that is updated, its database parameters and its children are tracked
    [Theory]
    [InlineData("AppA")]
    [InlineData("APPA")]
    public async Task GetByNameForUpdate_FindsTheNameWithoutCaseAndTracksOnlyThatProjectWithItsParts(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Project found = await ReadForUpdate(context, name);

        Assert.Equal(_appA.Id, found.Id);
        Assert.Equal(2, found.GitRepos.Count);
        Assert.Equal(EntityState.Unchanged, context.Entry(found).State);
        Assert.Equal(1 + 2 + 7, context.ChangeTracker.Entries().Count());
    }

    [Fact]
    public async Task GetByNameForUpdate_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new ProjectRepository(context).GetByNameForUpdate("AppZ", CancellationToken.None));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //A missing part stays missing, and a part whose texts are all missing still exists: its required columns tell
    //them apart
    [Fact]
    public async Task Add_StoresAMissingAndAnEmptyDatabaseParametersPartApart()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ProjectRepository(context).Add(TestData.NewProject("AppC", prodCopyDatabaseParameters:
                new DatabaseParameters(null, null, null, null, null, null, 0, false, null, null, null, null, null,
                    null, null)));
            await context.SaveChangesAsync();
        }

        Project? stored = await Stored("AppC");
        Assert.NotNull(stored);
        Assert.Null(stored.DevDatabaseParameters);
        DatabaseParameters prodCopy = Assert.IsType<DatabaseParameters>(stored.ProdCopyDatabaseParameters);
        Assert.Null(prodCopy.DbConnectionId);
        Assert.Null(prodCopy.DatabaseName);
        Assert.Equal(0, prodCopy.CommandTimeOut);
        Assert.False(prodCopy.SkipBackupBeforeRestore);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ProjectRepository(context).Add(TestData.NewProject("AppA"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //A git may be in a project once in each role
    [Fact]
    public async Task Add_StoresTheSameGitInBothRoles()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ProjectRepository(context).Add(TestData.NewProject("AppC", gitRepos: [_repoA],
                scaffoldSeederGitRepos: [_repoA]));
            await context.SaveChangesAsync();
        }

        Project? stored = await Stored("AppC");
        Assert.NotNull(stored);
        Assert.Equal([EProjectGitRepoKind.Main, EProjectGitRepoKind.ScaffoldSeed],
            stored.GitRepos.Select(x => x.Kind).Order());
    }

    //The unique indexes of the children keep one child of a key per project
    [Theory]
    [InlineData("git")]
    [InlineData("npm")]
    [InlineData("file")]
    [InlineData("tool")]
    [InlineData("endpoint")]
    [InlineData("route")]
    public async Task Add_IsRefusedOnSave_WhenAChildKeyRepeatsInTheProject(string child)
    {
        Project project = TestData.NewProject("AppC", gitRepos: child == "git" ? [_repoA, _repoA] : [],
            npmPackages: child == "npm" ? [_react, _react] : []);
        project.Update("AppC", "Standard", null, null, 1, 0, false, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, project.GitRepos, project.NpmPackages,
            child == "file" ? [ProjectRedundantFile.Create("a"), ProjectRedundantFile.Create("a")] : [],
            child == "tool" ? [ProjectAllowedTool.Create("a"), ProjectAllowedTool.Create("a")] : [],
            child == "endpoint"
                ?
                [
                    ProjectEndpoint.Create("a", null, null, false, "Get", "Query", null, false),
                    ProjectEndpoint.Create("a", null, null, false, "Get", "Query", null, false)
                ]
                : [],
            child == "route"
                ? [ProjectRouteClass.Create("a", null, null, null), ProjectRouteClass.Create("a", null, null, null)]
                : []);
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ProjectRepository(context).Add(project);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The references of the children and the database parameters are foreign keys: a missing record is refused
    [Fact]
    public async Task Add_IsRefusedOnSave_WhenAReferencedRecordDoesNotExist()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ProjectRepository(context).Add(TestData.NewProject("AppC",
            devDatabaseParameters: TestData.NewDatabaseParameters(TestData.NewDatabaseServerConnection("Pc9.Sql"))));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The update replaces the children: the replaced rows are deleted and no orphan is left, even for a child with the
    //same key, whose old row goes before the new one is inserted. The database parameters change in the same row
    [Fact]
    public async Task Update_ReplacesTheChildrenAndTheDatabaseParametersOfTheReadProjectAndStoresItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            Project read = await ReadForUpdate(context, "AppA");
            Update(read, null, TestData.NewDatabaseParameters(databaseName: "Other"),
                [ProjectGitRepo.Create(_repoB.Id, EProjectGitRepoKind.Main)], [],
                [ProjectRedundantFile.Create("*.pdb"), ProjectRedundantFile.Create("*.xml")]);
            new ProjectRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        Project? stored = await Stored("AppA");
        Assert.NotNull(stored);
        Assert.Equal(_appA.Id, stored.Id);
        Assert.Null(stored.DevDatabaseParameters);
        DatabaseParameters prodCopy = Assert.IsType<DatabaseParameters>(stored.ProdCopyDatabaseParameters);
        Assert.Null(prodCopy.DbConnectionId);
        Assert.Equal("Other", prodCopy.DatabaseName);
        Assert.Equal([(_repoB.Id, EProjectGitRepoKind.Main)], stored.GitRepos.Select(x => (x.GitRepoId, x.Kind)));
        Assert.Empty(stored.NpmPackages);
        Assert.Equal(["*.pdb", "*.xml"], stored.RedundantFiles.Select(x => x.FileName).Order());
        Assert.Empty(stored.Endpoints);
        Assert.Equal("v2", Assert.Single(stored.RouteClasses).ApiVersion);
        Assert.Equal(2, stored.Version);
        Assert.Equal(
        [
            "endpoint Upload", "file *.pdb", "file *.pdb", "file *.xml", "git Main", "route Main", "route Main",
            "tool SeedData", "tool SeedData"
        ], await StoredChildRows());
    }

    //A missing part of the stored project gets its values
    [Fact]
    public async Task Update_AddsTheDatabaseParameters_WhenTheStoredProjectHasNone()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            Project read = await ReadForUpdate(context, "AppB");
            Update(read, TestData.NewDatabaseParameters(_connection, _smartSchema), null, [], [], []);
            new ProjectRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        Project? stored = await Stored("AppB");
        Assert.NotNull(stored);
        DatabaseParameters dev = Assert.IsType<DatabaseParameters>(stored.DevDatabaseParameters);
        Assert.Equal(_connection.Id, dev.DbConnectionId);
        Assert.Equal(_smartSchema.Id, dev.SmartSchemaId);
        Assert.Null(stored.ProdCopyDatabaseParameters);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing, neither the project nor its
    //children
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheProjectChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        Project readFirst = await ReadForUpdate(first, "AppA");
        Project readSecond = await ReadForUpdate(second, "AppA");
        Update(readFirst, null, null, [], [], [ProjectRedundantFile.Create("first")]);
        new ProjectRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        Update(readSecond, TestData.NewDatabaseParameters(), null, [], [], [ProjectRedundantFile.Create("second")]);
        new ProjectRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Project? stored = await Stored("AppA");
        Assert.NotNull(stored);
        Assert.Null(stored.DevDatabaseParameters);
        Assert.Equal(["first"], stored.RedundantFiles.Select(x => x.FileName));
        Assert.Equal(2, stored.Version);
        Assert.Contains("file first", await StoredChildRows());
        Assert.DoesNotContain("file second", await StoredChildRows());
    }

    //Update relies on the one version increment of Project.Update: it expects the stored version to be one less than
    //the version of the instance, so an instance whose version did not grow is refused
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheVersionOfTheReadProjectDidNotGrow()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        Project read = await ReadForUpdate(context, "AppA");
        new ProjectRepository(context).Update(read);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());

        Project? stored = await Stored("AppA");
        Assert.NotNull(stored);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheProjectWithItsChildrenOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ProjectRepository(context).Delete(await Read(context, "AppA"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["AppB"], await check.Projects.Select(x => x.Name).ToListAsync());
        Assert.Equal(["endpoint Upload", "file *.pdb", "route Main", "tool SeedData"], await StoredChildRows());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheProjectChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        Project readFirst = await ReadForUpdate(first, "AppA");
        Project readSecond = await Read(second, "AppA");
        Update(readFirst, null, null, [], [], []);
        new ProjectRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new ProjectRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("AppA"));
    }

    //Every reference of a project is restricted: the database refuses to delete a record that a project uses
    [Fact]
    public async Task TheDatabase_RefusesToDeleteTheRecordsThatAProjectUses()
    {
        foreach (Func<SupportToolsServerDbContext, object> referenced in new Func<SupportToolsServerDbContext, object>[]
                 {
                     c => c.EditorConfigFileTypes.Single(x => x.Name == "default"),
                     c => c.GitRepos.Single(x => x.Name == "RepoA"), c => c.GitRepos.Single(x => x.Name == "RepoB"),
                     c => c.NpmPackages.Single(), c => c.DatabaseServerConnections.Single(),
                     c => c.SmartSchemas.Single(), c => c.FileStorages.Single()
                 })
        {
            await using SupportToolsServerDbContext context = _database.NewContext();
            context.Remove(referenced(context));

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }
}
