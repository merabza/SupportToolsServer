using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.ApiClients.DeleteApiClient;
using SupportToolsServer.Application.FileStorages.DeleteFileStorage;
using SupportToolsServer.Application.Settings.GetGlobalSettings;
using SupportToolsServer.Application.Settings.UpdateGlobalSettings;
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
//show the singleton (the first create, a second first create, the version check between the read and the save), its
//exchange parameters in the same row, and both sides of its references: the settings cannot name a missing record,
//and a record that they use cannot be deleted
public sealed class GlobalSettingsHandlersOnSqliteTests : IAsyncLifetime
{
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.FileStorages.AddRange(TestData.NewFileStorage("Exchange"), TestData.NewFileStorage("Backups"));
        context.SmartSchemas.AddRange(TestData.NewSmartSchema("Reduce"), TestData.NewSmartSchema("Keep"));
        context.ApiClients.Add(TestData.NewApiClient("Bagetter"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private async Task<Result<int>> Upsert(StsGlobalSettingsDataModel model, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateGlobalSettingsCommandHandler(new GlobalSettingsRepository(context),
            new FileStorageRepository(context), new SmartSchemaRepository(context), new ApiClientRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new UpdateGlobalSettingsCommand(model), CancellationToken.None);
    }

    private async Task<Result<StsGlobalSettingsDataModel>> Get()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new GetGlobalSettingsQueryHandler(new GlobalSettingsRepository(context),
            new FileStorageRepository(context), new SmartSchemaRepository(context), new ApiClientRepository(context));
        return await handler.Handle(new GetGlobalSettingsQuery(), CancellationToken.None);
    }

    private async Task<Result> DeleteFileStorage(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteFileStorageCommandHandler(new FileStorageRepository(context),
            new GlobalSettingsRepository(context), new ProjectCreatorSettingsRepository(context),
            new ProjectRepository(context), new ServerRepository(context),
            new DeploymentEnvironmentRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteFileStorageCommand(name, null), CancellationToken.None);
    }

    private async Task<Result> DeleteSmartSchema(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteSmartSchemaCommandHandler(new SmartSchemaRepository(context),
            new GlobalSettingsRepository(context), new ProjectCreatorSettingsRepository(context),
            new ProjectRepository(context), new ServerRepository(context),
            new DeploymentEnvironmentRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteSmartSchemaCommand(name, null), CancellationToken.None);
    }

    private async Task<Result> DeleteApiClient(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteApiClientCommandHandler(new ApiClientRepository(context),
            new DatabaseServerConnectionRepository(context), new ServerRepository(context),
            new GlobalSettingsRepository(context), new ProjectRepository(context),
            new DeploymentEnvironmentRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteApiClientCommand(name, null), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<GlobalSettings>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.GlobalSettings.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Get_ReturnsAnEmptyContractWithVersionZero_BeforeTheFirstCreate()
    {
        Result<StsGlobalSettingsDataModel> result = await Get();

        Assert.Equal(0, result.Value.Version);
        Assert.Null(result.Value.MediatRLicenseKey);
        Assert.NotNull(result.Value.DatabasesBackupFilesExchange);
        Assert.Empty(await Stored());
    }

    //The references match without case and are read back with the stored names
    [Fact]
    public async Task Upsert_CreatesTheSingletonAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert(TestData.GlobalSettingsModel("EXCHANGE", "reduce", "Keep", "BAGETTER",
            "Backups", "Keep", "Reduce"))).Value);
        StsGlobalSettingsDataModel update = TestData.GlobalSettingsModel("Backups", version: 1);
        update.ServiceDescriptionSignature = "new";
        Assert.Equal(2, (await Upsert(update)).Value);
        Assert.Equal(3, (await Upsert(TestData.GlobalSettingsModel("Exchange", "Reduce", "Keep", "Bagetter",
            "Backups", "Keep", "Reduce", 2))).Value);

        GlobalSettings stored = Assert.Single(await Stored());
        Assert.Equal(GlobalSettingsId.Singleton, stored.Id);
        Assert.Equal(3, stored.Version);
        StsGlobalSettingsDataModel read = (await Get()).Value;
        Assert.Equal("ltgmz", read.ServiceDescriptionSignature);
        Assert.Equal(TestData.MadeUpLicenseKey, read.MediatRLicenseKey);
        Assert.Equal("Exchange", read.FileStorageNameForExchange);
        Assert.Equal("Reduce", read.SmartSchemaNameForExchange);
        Assert.Equal("Keep", read.SmartSchemaNameForLocal);
        Assert.Equal("Bagetter", read.LocalPackageManagerWebApiClientName);
        Assert.Equal(".down!", read.DatabasesBackupFilesExchange.DownloadTempExtension);
        Assert.Equal(".up!", read.DatabasesBackupFilesExchange.UploadTempExtension);
        Assert.Equal("Backups", read.DatabasesBackupFilesExchange.ExchangeFileStorageName);
        Assert.Equal("Keep", read.DatabasesBackupFilesExchange.ExchangeSmartSchemaName);
        Assert.Equal("Reduce", read.DatabasesBackupFilesExchange.LocalSmartSchemaName);
        Assert.Equal(3, read.Version);
    }

    //The exchange parameters are required: with every column NULL they are still read as an empty part
    [Fact]
    public async Task Upsert_StoresEmptyExchangeParameters()
    {
        Assert.Equal(1, (await Upsert(new StsGlobalSettingsDataModel())).Value);

        GlobalSettings stored = Assert.Single(await Stored());
        Assert.NotNull(stored.DatabasesBackupFilesExchange);
        Assert.Null(stored.DatabasesBackupFilesExchange.UploadTempExtension);
        Assert.Null(stored.DatabasesBackupFilesExchange.ExchangeFileStorageId);
        StsGlobalSettingsDataModel read = (await Get()).Value;
        Assert.Null(read.DatabasesBackupFilesExchange.DownloadTempExtension);
        Assert.Equal(1, read.Version);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAFirstCreateFindsTheSingleton()
    {
        await Upsert(TestData.GlobalSettingsModel());

        Result<int> result = await Upsert(TestData.GlobalSettingsModel("Exchange"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Settings Global Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Null(Assert.Single(await Stored()).FileStorageForExchangeId);
    }

    //Two first creates at once: the fixed key stops the second INSERT and the singleton stays one record
    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheSingletonBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert(TestData.GlobalSettingsModel("Exchange"),
            async () => Assert.Equal(1, (await Upsert(TestData.GlobalSettingsModel("Backups"))).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Settings Global Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal("Backups", (await Get()).Value.FileStorageNameForExchange);
        Assert.Single(await Stored());
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert(TestData.GlobalSettingsModel());

        Result<int> result = await Upsert(TestData.GlobalSettingsModel("Exchange", version: 1),
            async () => Assert.Equal(2, (await Upsert(TestData.GlobalSettingsModel("Backups", version: 1))).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Settings Global Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("Backups", (await Get()).Value.FileStorageNameForExchange);
    }

    //The references are checked before anything is written
    [Fact]
    public async Task Upsert_ReturnsReferencedRecordsNotFoundWithEveryMissingName()
    {
        Result<int> result = await Upsert(TestData.GlobalSettingsModel("Missing", "Reduce", "Hourly",
            "Pc9.Packages", localSmartSchemaName: "Weekly"));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal("Referenced FileStorage Records Not Found: Missing; " +
                     "Referenced SmartSchema Records Not Found: Hourly, Weekly; " +
                     "Referenced ApiClient Records Not Found: Pc9.Packages", result.Error.Description);
        Assert.Empty(await Stored());
    }

    //The foreign key of the exchange parameters keeps the file storage that was deleted after the check; the version
    //still matches, so the save fails with the exception of the database (CLAUDE.md, Registry conventions: a 500)
    [Fact]
    public async Task Upsert_Throws_WhenAnotherRequestDeletesAReferencedRecordBetweenTheCheckAndTheSave()
    {
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Upsert(TestData.GlobalSettingsModel(exchangeFileStorageName: "Backups"),
                async () => Assert.True((await DeleteFileStorage("Backups")).IsSuccess)));

        Assert.Empty(await Stored());
    }

    //The file storage stays while a field of the settings names it, and can be deleted once none does
    [Fact]
    public async Task DeleteFileStorage_ReturnsRecordIsInUse_UntilTheSettingsNoLongerUseIt()
    {
        await Upsert(TestData.GlobalSettingsModel("Exchange", exchangeFileStorageName: "Exchange"));

        Result refused = await DeleteFileStorage("exchange");
        await Upsert(TestData.GlobalSettingsModel(exchangeFileStorageName: "Exchange", version: 1));
        Result stillRefused = await DeleteFileStorage("Exchange");
        await Upsert(TestData.GlobalSettingsModel(version: 2));
        Result deleted = await DeleteFileStorage("Exchange");

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal(
            "FileStorage exchange Is Used By: GlobalSettings.FileStorageNameForExchange, " +
            "GlobalSettings.DatabasesBackupFilesExchange.ExchangeFileStorageName", refused.Error.Description);
        Assert.Equal(
            "FileStorage Exchange Is Used By: GlobalSettings.DatabasesBackupFilesExchange.ExchangeFileStorageName",
            stillRefused.Error.Description);
        Assert.True(deleted.IsSuccess);
    }

    [Fact]
    public async Task DeleteSmartSchema_ReturnsRecordIsInUse_UntilTheSettingsNoLongerUseIt()
    {
        await Upsert(TestData.GlobalSettingsModel(smartSchemaNameForLocal: "Keep", localSmartSchemaName: "Keep"));

        Result refused = await DeleteSmartSchema("KEEP");
        await Upsert(TestData.GlobalSettingsModel(smartSchemaNameForLocal: "Reduce", version: 1));
        Result deleted = await DeleteSmartSchema("Keep");

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal(
            "SmartSchema KEEP Is Used By: GlobalSettings.SmartSchemaNameForLocal, " +
            "GlobalSettings.DatabasesBackupFilesExchange.LocalSmartSchemaName", refused.Error.Description);
        Assert.True(deleted.IsSuccess);
        Assert.Equal("RecordIsInUse", (await DeleteSmartSchema("Reduce")).Error.Code);
    }

    [Fact]
    public async Task DeleteApiClient_ReturnsRecordIsInUse_UntilTheSettingsNoLongerUseIt()
    {
        await Upsert(TestData.GlobalSettingsModel(localPackageManagerWebApiClientName: "Bagetter"));

        Result refused = await DeleteApiClient("bagetter");
        await Upsert(TestData.GlobalSettingsModel(version: 1));
        Result deleted = await DeleteApiClient("Bagetter");

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal("ApiClient bagetter Is Used By: GlobalSettings.LocalPackageManagerWebApiClientName",
            refused.Error.Description);
        Assert.True(deleted.IsSuccess);
    }
}
