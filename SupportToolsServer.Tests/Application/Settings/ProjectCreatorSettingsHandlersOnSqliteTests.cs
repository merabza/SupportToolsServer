using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;
using SupportToolsServer.Application.Environments.DeleteEnvironment;
using SupportToolsServer.Application.FileStorages.DeleteFileStorage;
using SupportToolsServer.Application.Servers.DeleteServer;
using SupportToolsServer.Application.Settings.GetProjectCreatorSettings;
using SupportToolsServer.Application.Settings.UpdateGlobalSettings;
using SupportToolsServer.Application.Settings.UpdateProjectCreatorSettings;
using SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Settings;

//The handlers with the real repositories and unit of work on SQLite, one context per request as in the host. They
//show the singleton (the first create, the version check between the read and the save) and both sides of its
//references: the settings cannot name a missing record, and a record that they use cannot be deleted
public sealed class ProjectCreatorSettingsHandlersOnSqliteTests : IAsyncLifetime
{
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.Servers.AddRange(TestData.NewServer("dl360"), TestData.NewServer("PAZISI"));
        context.Environments.AddRange(TestData.NewEnvironment("Prod"), TestData.NewEnvironment("Stage"));
        context.DatabaseServerConnections.Add(TestData.NewDatabaseServerConnection("Pazisi"));
        context.FileStorages.Add(TestData.NewFileStorage("Backups"));
        context.SmartSchemas.Add(TestData.NewSmartSchema("Reduce"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private async Task<Result<int>> Upsert(StsProjectCreatorSettingsDataModel model,
        Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateProjectCreatorSettingsCommandHandler(new ProjectCreatorSettingsRepository(context),
            new ServerRepository(context), new DeploymentEnvironmentRepository(context),
            new DatabaseServerConnectionRepository(context), new FileStorageRepository(context),
            new SmartSchemaRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(new UpdateProjectCreatorSettingsCommand(model), CancellationToken.None);
    }

    private async Task<Result<int>> UpsertGlobalSettings(StsGlobalSettingsDataModel model)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateGlobalSettingsCommandHandler(new GlobalSettingsRepository(context),
            new FileStorageRepository(context), new SmartSchemaRepository(context), new ApiClientRepository(context),
            new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new UpdateGlobalSettingsCommand(model), CancellationToken.None);
    }

    private async Task<Result<StsProjectCreatorSettingsDataModel>> Get()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new GetProjectCreatorSettingsQueryHandler(new ProjectCreatorSettingsRepository(context),
            new ServerRepository(context), new DeploymentEnvironmentRepository(context),
            new DatabaseServerConnectionRepository(context), new FileStorageRepository(context),
            new SmartSchemaRepository(context));
        return await handler.Handle(new GetProjectCreatorSettingsQuery(), CancellationToken.None);
    }

    private async Task<Result> DeleteServer(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteServerCommandHandler(new ServerRepository(context),
            new ProjectCreatorSettingsRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteServerCommand(name, null), CancellationToken.None);
    }

    private async Task<Result> DeleteEnvironment(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteEnvironmentCommandHandler(new DeploymentEnvironmentRepository(context),
            new ProjectCreatorSettingsRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteEnvironmentCommand(name, null), CancellationToken.None);
    }

    private async Task<Result> DeleteDatabaseServerConnection(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteDatabaseServerConnectionCommandHandler(new DatabaseServerConnectionRepository(context),
            new ProjectCreatorSettingsRepository(context), new ProjectRepository(context),
            new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteDatabaseServerConnectionCommand(name, null), CancellationToken.None);
    }

    private async Task<Result> DeleteFileStorage(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteFileStorageCommandHandler(new FileStorageRepository(context),
            new GlobalSettingsRepository(context), new ProjectCreatorSettingsRepository(context),
            new ProjectRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteFileStorageCommand(name, null), CancellationToken.None);
    }

    private async Task<Result> DeleteSmartSchema(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteSmartSchemaCommandHandler(new SmartSchemaRepository(context),
            new GlobalSettingsRepository(context), new ProjectCreatorSettingsRepository(context),
            new ProjectRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteSmartSchemaCommand(name, null), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<ProjectCreatorSettings>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.ProjectCreatorSettings.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Get_ReturnsAnEmptyContractWithVersionZero_BeforeTheFirstCreate()
    {
        Result<StsProjectCreatorSettingsDataModel> result = await Get();

        Assert.Equal(0, result.Value.Version);
        Assert.Null(result.Value.ProjectsFolderPathReal);
        Assert.Empty(await Stored());
    }

    //The references match without case and are read back with the stored names
    [Fact]
    public async Task Upsert_CreatesTheSingletonAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1,
            (await Upsert(TestData.ProjectCreatorSettingsModel("DL360", "prod", "PAZISI", "backups", "REDUCE")))
            .Value);
        Assert.Equal(2, (await Upsert(TestData.ProjectCreatorSettingsModel("PAZISI", "Stage", version: 1))).Value);

        ProjectCreatorSettings stored = Assert.Single(await Stored());
        Assert.Equal(ProjectCreatorSettingsId.Singleton, stored.Id);
        StsProjectCreatorSettingsDataModel read = (await Get()).Value;
        Assert.Equal(4, read.IndentSize);
        Assert.Equal("FakeHost", read.FakeHostProjectName);
        Assert.Equal(@"D:\1WorkDotnet", read.ProjectsFolderPathReal);
        Assert.Equal(@"D:\1WorkSecurity", read.SecretsFolderPathReal);
        Assert.Equal("PAZISI", read.ProductionServerName);
        Assert.Equal("Stage", read.ProductionEnvironmentName);
        Assert.Null(read.DeveloperDbConnectionName);
        Assert.Null(read.DatabaseExchangeFileStorageName);
        Assert.Null(read.UseSmartSchema);
        Assert.Equal(2, read.Version);
    }

    //Two first creates at once: the fixed key stops the second INSERT and the singleton stays one record
    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheSingletonBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert(TestData.ProjectCreatorSettingsModel("dl360"),
            async () => Assert.Equal(1, (await Upsert(TestData.ProjectCreatorSettingsModel("PAZISI"))).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Settings ProjectCreator Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal("PAZISI", (await Get()).Value.ProductionServerName);
        Assert.Single(await Stored());
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert(TestData.ProjectCreatorSettingsModel());

        Result<int> result = await Upsert(TestData.ProjectCreatorSettingsModel("dl360", version: 1),
            async () => Assert.Equal(2,
                (await Upsert(TestData.ProjectCreatorSettingsModel("PAZISI", version: 1))).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Settings ProjectCreator Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("PAZISI", (await Get()).Value.ProductionServerName);
    }

    //The references are checked before anything is written
    [Fact]
    public async Task Upsert_ReturnsReferencedRecordsNotFoundWithEveryMissingName()
    {
        Result<int> result =
            await Upsert(TestData.ProjectCreatorSettingsModel("guria", "Prod", "Pc9.Sql", "Missing", "Hourly"));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal("Referenced Server Records Not Found: guria; " +
                     "Referenced DatabaseServerConnection Records Not Found: Pc9.Sql; " +
                     "Referenced FileStorage Records Not Found: Missing; " +
                     "Referenced SmartSchema Records Not Found: Hourly", result.Error.Description);
        Assert.Empty(await Stored());
    }

    //The foreign key keeps the server that was deleted after the check; the version still matches, so the save fails
    //with the exception of the database (CLAUDE.md, Registry conventions: a 500)
    [Fact]
    public async Task Upsert_Throws_WhenAnotherRequestDeletesAReferencedRecordBetweenTheCheckAndTheSave()
    {
        await Assert.ThrowsAsync<DbUpdateException>(() => Upsert(TestData.ProjectCreatorSettingsModel("dl360"),
            async () => Assert.True((await DeleteServer("dl360")).IsSuccess)));

        Assert.Empty(await Stored());
    }

    //The server stays while the settings name it, and can be deleted once they do not
    [Fact]
    public async Task DeleteServer_ReturnsRecordIsInUse_UntilTheSettingsNoLongerUseIt()
    {
        await Upsert(TestData.ProjectCreatorSettingsModel("dl360"));

        Result refused = await DeleteServer("DL360");
        await Upsert(TestData.ProjectCreatorSettingsModel(version: 1));
        Result deleted = await DeleteServer("dl360");

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal("Server DL360 Is Used By: ProjectCreatorSettings.ProductionServerName", refused.Error.Description);
        Assert.True(deleted.IsSuccess);
    }

    [Fact]
    public async Task DeleteEnvironment_ReturnsRecordIsInUse_UntilTheSettingsNoLongerUseIt()
    {
        await Upsert(TestData.ProjectCreatorSettingsModel(productionEnvironmentName: "Prod"));

        Result refused = await DeleteEnvironment("prod");
        await Upsert(TestData.ProjectCreatorSettingsModel(productionEnvironmentName: "Stage", version: 1));
        Result deleted = await DeleteEnvironment("Prod");

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal("Environment prod Is Used By: ProjectCreatorSettings.ProductionEnvironmentName",
            refused.Error.Description);
        Assert.True(deleted.IsSuccess);
        Assert.Equal("RecordIsInUse", (await DeleteEnvironment("Stage")).Error.Code);
    }

    [Fact]
    public async Task DeleteDatabaseServerConnection_ReturnsRecordIsInUse_UntilTheSettingsNoLongerUseIt()
    {
        await Upsert(TestData.ProjectCreatorSettingsModel(developerDbConnectionName: "Pazisi"));

        Result refused = await DeleteDatabaseServerConnection("pazisi");
        await Upsert(TestData.ProjectCreatorSettingsModel(version: 1));
        Result deleted = await DeleteDatabaseServerConnection("Pazisi");

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal("DatabaseServerConnection pazisi Is Used By: ProjectCreatorSettings.DeveloperDbConnectionName",
            refused.Error.Description);
        Assert.True(deleted.IsSuccess);
    }

    //Both singletons may use one file storage: the global settings are named first
    [Fact]
    public async Task DeleteFileStorage_ReturnsRecordIsInUseWithTheFieldsOfBothSettings()
    {
        await Upsert(TestData.ProjectCreatorSettingsModel(databaseExchangeFileStorageName: "Backups"));
        await UpsertGlobalSettings(TestData.GlobalSettingsModel("Backups"));

        Result refused = await DeleteFileStorage("Backups");
        await Upsert(TestData.ProjectCreatorSettingsModel(version: 1));
        Result stillRefused = await DeleteFileStorage("Backups");

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal(
            "FileStorage Backups Is Used By: GlobalSettings.FileStorageNameForExchange, " +
            "ProjectCreatorSettings.DatabaseExchangeFileStorageName", refused.Error.Description);
        Assert.Equal("FileStorage Backups Is Used By: GlobalSettings.FileStorageNameForExchange",
            stillRefused.Error.Description);
    }

    [Fact]
    public async Task DeleteSmartSchema_ReturnsRecordIsInUse_UntilTheSettingsNoLongerUseIt()
    {
        await Upsert(TestData.ProjectCreatorSettingsModel(useSmartSchema: "Reduce"));

        Result refused = await DeleteSmartSchema("Reduce");
        await Upsert(TestData.ProjectCreatorSettingsModel(version: 1));
        Result deleted = await DeleteSmartSchema("Reduce");

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal("SmartSchema Reduce Is Used By: ProjectCreatorSettings.UseSmartSchema", refused.Error.Description);
        Assert.True(deleted.IsSuccess);
    }
}
