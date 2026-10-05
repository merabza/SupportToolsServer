using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;
using SupportToolsServer.Application.EditorConfigFileTypes.DeleteEditorConfigFileType;
using SupportToolsServer.Application.EditorConfigFileTypes.SyncUp;
using SupportToolsServer.Application.FileStorages.DeleteFileStorage;
using SupportToolsServer.Application.GitRepos.DeleteGitRepo;
using SupportToolsServer.Application.NpmPackages.DeleteNpmPackage;
using SupportToolsServer.Application.Projects.DeleteProject;
using SupportToolsServer.Application.Projects.GetProjectByName;
using SupportToolsServer.Application.Projects.GetProjects;
using SupportToolsServer.Application.Projects.UpdateProject;
using SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Projects;

//The handlers with the real repositories and unit of work on SQLite, one context per request as in the host.
//Besides the versions of the aggregate and the replacement of its children, they show the references of both sides:
//a project cannot name a missing record, and a record that a project uses cannot be deleted
public sealed class ProjectHandlersOnSqliteTests : IAsyncLifetime
{
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        var gitIgnoreFileType = TestData.NewGitIgnoreFileType("CSharp");
        context.GitIgnoreFileTypes.Add(gitIgnoreFileType);
        context.GitRepos.AddRange(TestData.NewGitRepo("RepoA", gitIgnoreFileType),
            TestData.NewGitRepo("RepoB", gitIgnoreFileType));
        context.EditorConfigFileTypes.AddRange(TestData.NewEditorConfigFileType("default"),
            TestData.NewEditorConfigFileType("strict"));
        context.NpmPackages.AddRange(TestData.NewNpmPackage("react"), TestData.NewNpmPackage("@reduxjs/toolkit"));
        context.DatabaseServerConnections.Add(TestData.NewDatabaseServerConnection("Pc1.Sql"));
        context.SmartSchemas.Add(TestData.NewSmartSchema("Reduce"));
        context.FileStorages.Add(TestData.NewFileStorage("Backups"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    //A project that uses every kind of reference
    private static StsProjectDataModel ModelWithReferences(int version)
    {
        return TestData.ProjectModel("AppA", "default", TestData.DatabaseParametersModel("Pc1.Sql", "Reduce"),
            TestData.DatabaseParametersModel("Pc1.Sql", null, "Backups", "AppAProdCopy"), ["RepoA"], ["RepoB"],
            ["@reduxjs/toolkit", "react"], version);
    }

    private async Task<Result<int>> Upsert(StsProjectDataModel model, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateProjectCommandHandler(new ProjectRepository(context),
            new EditorConfigFileTypeRepository(context), new DatabaseServerConnectionRepository(context),
            new SmartSchemaRepository(context), new FileStorageRepository(context), new GitRepoRepository(context),
            new NpmPackageRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(new UpdateProjectCommand(model), CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteProjectCommandHandler(new ProjectRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteProjectCommand(name, version), CancellationToken.None);
    }

    private async Task<Result<StsProjectDataModel>> Get(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new GetProjectByNameQueryHandler(new ProjectRepository(context),
            new EditorConfigFileTypeRepository(context), new DatabaseServerConnectionRepository(context),
            new SmartSchemaRepository(context), new FileStorageRepository(context), new GitRepoRepository(context),
            new NpmPackageRepository(context));
        return await handler.Handle(new GetProjectByNameQuery(name), CancellationToken.None);
    }

    private async Task<Result<List<StsProjectDataModel>>> GetAll()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new GetProjectsQueryHandler(new ProjectRepository(context),
            new EditorConfigFileTypeRepository(context), new DatabaseServerConnectionRepository(context),
            new SmartSchemaRepository(context), new FileStorageRepository(context), new GitRepoRepository(context),
            new NpmPackageRepository(context));
        return await handler.Handle(new GetProjectsQuery(), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<Project>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await new ProjectRepository(context).GetAll(CancellationToken.None);
    }

    //Every child row of the database, so that orphans would show
    private async Task<int> StoredChildRowCount()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.Set<ProjectGitRepo>().CountAsync() + await context.Set<ProjectNpmPackage>().CountAsync() +
               await context.Set<ProjectRedundantFile>().CountAsync() +
               await context.Set<ProjectAllowedTool>().CountAsync() +
               await context.Set<ProjectEndpoint>().CountAsync() + await context.Set<ProjectRouteClass>().CountAsync();
    }

    //What the client uploads comes back unchanged, with the new version
    [Fact]
    public async Task Upsert_CreatesTheProjectThatTheGetReturnsAsItWasSent()
    {
        StsProjectDataModel sent = ModelWithReferences(0);

        Result<int> created = await Upsert(sent);

        Assert.Equal(1, created.Value);
        sent.Version = 1;
        sent.FrontNpmPackageNames = ["@reduxjs/toolkit", "react"];
        Assert.Equal(JsonSerializer.Serialize(sent), JsonSerializer.Serialize((await Get("appa")).Value));
        Assert.Equal(["AppA"], (await GetAll()).Value.Select(x => x.Name));
    }

    //The update replaces the whole aggregate: children are added, removed and changed, the database parameters are
    //replaced or removed, and no orphan row is left
    [Fact]
    public async Task Upsert_ReplacesTheChildrenAndTheDatabaseParametersWithTheVersionsItReturns()
    {
        await Upsert(ModelWithReferences(0));
        StsProjectDataModel changed = TestData.ProjectModel("APPA", "strict", null,
            TestData.DatabaseParametersModel(null, "Reduce"), ["RepoB"], ["RepoB"], [], 1);
        changed.RedundantFileNames = ["*.pdb", "*.xml"];
        changed.AllowToolsList = [];
        changed.Endpoints[0].EndpointRoute = "/upload/v2";
        changed.RouteClasses = [new StsProjectRouteClassDataModel { Name = "Git", Base = "/git" }];

        Result<int> updated = await Upsert(changed);

        Assert.Equal(2, updated.Value);
        StsProjectDataModel read = (await Get("AppA")).Value;
        Assert.Equal("APPA", read.Name);
        Assert.Equal("strict", read.EditorConfigPatternName);
        Assert.Null(read.DevDatabaseParameters);
        Assert.Null(read.ProdCopyDatabaseParameters!.DbConnectionName);
        Assert.Equal("Reduce", read.ProdCopyDatabaseParameters.SmartSchemaName);
        Assert.Equal(["RepoB"], read.GitProjectNames);
        Assert.Equal(["RepoB"], read.ScaffoldSeederGitProjectNames);
        Assert.Empty(read.FrontNpmPackageNames);
        Assert.Equal(["*.pdb", "*.xml"], read.RedundantFileNames);
        Assert.Empty(read.AllowToolsList);
        Assert.Equal("/upload/v2", Assert.Single(read.Endpoints).EndpointRoute);
        Assert.Equal("Git", Assert.Single(read.RouteClasses).Name);
        Assert.Equal(2, read.Version);
        Assert.Equal(2 + 2 + 1 + 1, await StoredChildRowCount());
    }

    //The version of the root is the version of the aggregate: a change of a child alone gives a new version
    [Fact]
    public async Task Upsert_GivesANewVersion_WhenOnlyAChildChanges()
    {
        await Upsert(ModelWithReferences(0));
        StsProjectDataModel changed = ModelWithReferences(1);
        changed.Endpoints[0].ReturnType = "int";

        Assert.Equal(2, (await Upsert(changed)).Value);
        Assert.Equal("int", Assert.Single((await Get("AppA")).Value.Endpoints).ReturnType);
    }

    //The references are checked before anything is written
    [Fact]
    public async Task Upsert_ReturnsEveryMissingReferenceInOneError_AndWritesNothing()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA", "none",
            TestData.DatabaseParametersModel("Pc2.Sql", "Daily", "Exchange"), null, ["RepoA", "RepoZ"], [],
            ["left-pad"]);

        Result<int> result = await Upsert(model);

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal(
            "Referenced EditorConfigFileType Records Not Found: none; " +
            "Referenced DatabaseServerConnection Records Not Found: Pc2.Sql; " +
            "Referenced SmartSchema Records Not Found: Daily; Referenced FileStorage Records Not Found: Exchange; " +
            "Referenced GitRepo Records Not Found: RepoZ; Referenced NpmPackage Records Not Found: left-pad",
            result.Error.Description);
        Assert.Empty(await Stored());
        Assert.Equal(0, await StoredChildRowCount());
    }

    //Neither the project nor the children of the request that lost the race are written
    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert(ModelWithReferences(0));
        StsProjectDataModel mine = ModelWithReferences(1);
        mine.RedundantFileNames = ["mine"];
        StsProjectDataModel theirs = ModelWithReferences(1);
        theirs.RedundantFileNames = ["theirs"];

        Result<int> result = await Upsert(mine, async () => Assert.Equal(2, (await Upsert(theirs)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Project AppA Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal(["theirs"], (await Get("AppA")).Value.RedundantFileNames);
        Assert.Equal(2, Assert.Single(await Stored()).Version);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert(ModelWithReferences(0),
            async () => Assert.Equal(1, (await Upsert(TestData.ProjectModel("AppA"))).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Project AppA Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Empty((await Get("AppA")).Value.GitProjectNames);
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert(ModelWithReferences(0));

        Result<int> result = await Upsert(ModelWithReferences(1),
            async () => Assert.True((await Delete("AppA", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
        Assert.Equal(0, await StoredChildRowCount());
    }

    //A referenced record deleted between the check and the save fails the foreign key, which is not a version race,
    //so the exception propagates
    [Fact]
    public async Task Upsert_ThrowsTheSaveException_WhenAReferencedRecordIsDeletedBeforeTheSave()
    {
        await Assert.ThrowsAsync<DbUpdateException>(() => Upsert(ModelWithReferences(0), async () =>
        {
            await using SupportToolsServerDbContext context = _database.NewContext();
            context.NpmPackages.Remove(await context.NpmPackages.SingleAsync(x => x.Name == "react"));
            await context.SaveChangesAsync();
        }));

        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheProjectWithItsChildren_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert(ModelWithReferences(0));

        Result deleted = await Delete("APPA", 1);
        Result deletedAgain = await Delete("AppA", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal(0, await StoredChildRowCount());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    //Every record that a project uses stays until the project no longer uses it
    [Fact]
    public async Task DeleteOfAReferencedRecord_ReturnsRecordIsInUse_UntilNoProjectUsesIt()
    {
        await Upsert(ModelWithReferences(0));
        await Upsert(TestData.ProjectModel("AppB", "default", null, null, ["RepoA"], [], ["react"]));

        List<Result> refused = await DeleteEveryReferencedRecord();
        await Upsert(TestData.ProjectModel("AppA", version: 1));
        await Upsert(TestData.ProjectModel("AppB", version: 1));
        List<Result> deleted = await DeleteEveryReferencedRecord();

        Assert.Equal(
        [
            "GitRepo RepoA Is Used By: Project AppA, Project AppB", "GitRepo RepoB Is Used By: Project AppA",
            "NpmPackage react Is Used By: Project AppA, Project AppB",
            "NpmPackage @reduxjs/toolkit Is Used By: Project AppA",
            "EditorConfigFileType default Is Used By: Project AppA, Project AppB",
            "DatabaseServerConnection Pc1.Sql Is Used By: Project AppA", "SmartSchema Reduce Is Used By: Project AppA",
            "FileStorage Backups Is Used By: Project AppA"
        ], refused.Select(x => x.Error.Description));
        Assert.All(refused, x => Assert.Equal("RecordIsInUse", x.Error.Code));
        Assert.All(deleted, x => Assert.True(x.IsSuccess));
    }

    //Without merge the templates missing from the upload would be deleted, so a template in use stops the sync
    [Fact]
    public async Task SyncUpEditorConfigFileTypesWithoutMerge_ReturnsRecordIsInUse_WhenAProjectUsesAMissingTemplate()
    {
        await Upsert(ModelWithReferences(0));

        Result result = await SyncUpEditorConfigFileTypes([TestData.EditorConfigModel("strict")]);
        Result synced = await SyncUpEditorConfigFileTypes([TestData.EditorConfigModel("DEFAULT")]);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal("EditorConfigFileType default Is Used By: Project AppA", result.Error.Description);
        Assert.True(synced.IsSuccess);
        Assert.Equal("DEFAULT", (await Get("AppA")).Value.EditorConfigPatternName);
    }

    private async Task<Result> SyncUpEditorConfigFileTypes(List<StsEditorConfigFileTypeDataModel> uploaded)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new SyncUpEditorConfigFileTypesCommandHandler(new EditorConfigFileTypeRepository(context),
            new ProjectRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new SyncUpEditorConfigFileTypesCommand(false, uploaded), CancellationToken.None);
    }

    //The delete of every referenced record through its own handler, one request each
    private async Task<List<Result>> DeleteEveryReferencedRecord()
    {
        List<Result> results = [];
        foreach (string gitName in new[] { "RepoA", "RepoB" })
        {
            await using SupportToolsServerDbContext context = _database.NewContext();
            results.Add(await new DeleteGitRepoCommandHandler(new GitRepoRepository(context),
                new ProjectRepository(context), new SupportToolsServerUnitOfWork(context)).Handle(
                new DeleteGitRepoCommand(gitName), CancellationToken.None));
        }

        foreach (string npmPackageName in new[] { "react", "@reduxjs/toolkit" })
        {
            await using SupportToolsServerDbContext context = _database.NewContext();
            results.Add(await new DeleteNpmPackageCommandHandler(new NpmPackageRepository(context),
                new ProjectRepository(context), new SupportToolsServerUnitOfWork(context)).Handle(
                new DeleteNpmPackageCommand(npmPackageName, null), CancellationToken.None));
        }

        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            results.Add(await new DeleteEditorConfigFileTypeCommandHandler(new EditorConfigFileTypeRepository(context),
                new ProjectRepository(context), new SupportToolsServerUnitOfWork(context)).Handle(
                new DeleteEditorConfigFileTypeCommand("default"), CancellationToken.None));
        }

        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            results.Add(await new DeleteDatabaseServerConnectionCommandHandler(
                new DatabaseServerConnectionRepository(context), new ProjectCreatorSettingsRepository(context),
                new ProjectRepository(context), new SupportToolsServerUnitOfWork(context)).Handle(
                new DeleteDatabaseServerConnectionCommand("Pc1.Sql", null), CancellationToken.None));
        }

        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            results.Add(await new DeleteSmartSchemaCommandHandler(new SmartSchemaRepository(context),
                new GlobalSettingsRepository(context), new ProjectCreatorSettingsRepository(context),
                new ProjectRepository(context), new SupportToolsServerUnitOfWork(context)).Handle(
                new DeleteSmartSchemaCommand("Reduce", null), CancellationToken.None));
        }

        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            results.Add(await new DeleteFileStorageCommandHandler(new FileStorageRepository(context),
                new GlobalSettingsRepository(context), new ProjectCreatorSettingsRepository(context),
                new ProjectRepository(context), new SupportToolsServerUnitOfWork(context)).Handle(
                new DeleteFileStorageCommand("Backups", null), CancellationToken.None));
        }

        return results;
    }
}
