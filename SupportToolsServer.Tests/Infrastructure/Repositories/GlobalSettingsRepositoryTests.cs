using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

//The license keys are made up
public sealed class GlobalSettingsRepositoryTests : IAsyncLifetime
{
    private readonly FileStorage _backups = TestData.NewFileStorage("Backups");
    private readonly FileStorage _exchange = TestData.NewFileStorage("Exchange");
    private readonly SmartSchema _keep = TestData.NewSmartSchema("Keep");
    private readonly ApiClient _packages = TestData.NewApiClient("Bagetter");
    private readonly SmartSchema _reduce = TestData.NewSmartSchema("Reduce");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.FileStorages.AddRange(_exchange, _backups);
        context.SmartSchemas.AddRange(_reduce, _keep);
        context.ApiClients.Add(_packages);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private GlobalSettings NewWithEveryReference()
    {
        return TestData.NewGlobalSettings(_exchange, _reduce, _keep, _packages, _backups, _keep, _reduce);
    }

    private async Task Store(GlobalSettings globalSettings)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new GlobalSettingsRepository(context).Add(globalSettings);
        await context.SaveChangesAsync();
    }

    private static async Task<GlobalSettings> Read(SupportToolsServerDbContext context)
    {
        GlobalSettings? globalSettings = await new GlobalSettingsRepository(context).Get(CancellationToken.None);
        return Assert.IsType<GlobalSettings>(globalSettings);
    }

    private async Task<GlobalSettings> Stored()
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.GlobalSettings.AsNoTracking().SingleAsync();
    }

    [Fact]
    public async Task Get_ReturnsNull_BeforeTheSingletonIsCreated()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new GlobalSettingsRepository(context).Get(CancellationToken.None));
    }

    [Fact]
    public async Task Get_ReadsTheSingletonWithItsExchangeParametersWithoutTrackingIt()
    {
        await Store(NewWithEveryReference());
        await using SupportToolsServerDbContext context = _database.NewContext();

        GlobalSettings read = await Read(context);

        Assert.Equal(GlobalSettingsId.Singleton, read.Id);
        Assert.Equal("ltgmz", read.ServiceDescriptionSignature);
        Assert.Equal(".up!", read.UploadTempExtension);
        Assert.Equal("yyyyMMddHHmmss", read.ProgramArchiveDateMask);
        Assert.Equal(".zip", read.ProgramArchiveExtension);
        Assert.Equal("yyyyMMdd", read.ParametersFileDateMask);
        Assert.Equal(".json", read.ParametersFileExtension);
        Assert.Equal(TestData.MadeUpLicenseKey, read.MediatRLicenseKey);
        Assert.Equal(_exchange.Id, read.FileStorageForExchangeId);
        Assert.Equal(_reduce.Id, read.SmartSchemaForExchangeId);
        Assert.Equal(_keep.Id, read.SmartSchemaForLocalId);
        Assert.Equal(_packages.Id, read.LocalPackageManagerWebApiClientId);
        Assert.Equal(".down!", read.DatabasesBackupFilesExchange.DownloadTempExtension);
        Assert.Equal(".up!", read.DatabasesBackupFilesExchange.UploadTempExtension);
        Assert.Equal(_backups.Id, read.DatabasesBackupFilesExchange.ExchangeFileStorageId);
        Assert.Equal(_keep.Id, read.DatabasesBackupFilesExchange.ExchangeSmartSchemaId);
        Assert.Equal(_reduce.Id, read.DatabasesBackupFilesExchange.LocalSmartSchemaId);
        Assert.Equal(1, read.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //The part is required: with every column NULL it is read as an empty part, not as null
    [Fact]
    public async Task Get_ReadsEmptyExchangeParameters()
    {
        await Store(GlobalSettings.Create(null, null, null, null, null, null, null, null, null, null, null,
            new DatabasesBackupFilesExchange(null, null, null, null, null)));
        await using SupportToolsServerDbContext context = _database.NewContext();

        GlobalSettings read = await Read(context);

        Assert.NotNull(read.DatabasesBackupFilesExchange);
        Assert.Null(read.DatabasesBackupFilesExchange.DownloadTempExtension);
        Assert.Null(read.DatabasesBackupFilesExchange.LocalSmartSchemaId);
        Assert.Null(read.MediatRLicenseKey);
    }

    //The fixed key allows one record: a second first create fails on the primary key
    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheSingletonIsAlreadyStored()
    {
        await Store(NewWithEveryReference());
        await using SupportToolsServerDbContext context = _database.NewContext();
        new GlobalSettingsRepository(context).Add(TestData.NewGlobalSettings());

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The check constraint refuses any other key, even in the empty table
    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheKeyIsNotTheFixedOne()
    {
        var other = new GlobalSettings(new GlobalSettingsId(Guid.NewGuid()), null, null, null, null, null, null,
            null, null, null, null, null, new DatabasesBackupFilesExchange(null, null, null, null, null), 1);
        await using SupportToolsServerDbContext context = _database.NewContext();
        new GlobalSettingsRepository(context).Add(other);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.False(await check.GlobalSettings.AnyAsync());
    }

    //Each foreign key refuses a referenced record that is not stored, those of the exchange parameters too
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Add_IsRefusedOnSave_WhenAReferencedRecordIsNotStored(int missingReference)
    {
        FileStorage missingFileStorage = TestData.NewFileStorage("Missing");
        SmartSchema missingSmartSchema = TestData.NewSmartSchema("Hourly");
        GlobalSettings globalSettings = TestData.NewGlobalSettings(
            missingReference == 0 ? missingFileStorage : _exchange,
            missingReference == 1 ? missingSmartSchema : _reduce, missingReference == 2 ? missingSmartSchema : _keep,
            missingReference == 3 ? TestData.NewApiClient("Pc9.Packages") : _packages,
            missingReference == 4 ? missingFileStorage : _backups,
            missingReference == 5 ? missingSmartSchema : _keep, missingReference == 6 ? missingSmartSchema : _reduce);
        await using SupportToolsServerDbContext context = _database.NewContext();
        new GlobalSettingsRepository(context).Add(globalSettings);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The new exchange parameters replace the old ones in the same row
    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadSingletonWithItsNewVersion()
    {
        await Store(NewWithEveryReference());
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            GlobalSettings read = await Read(context);
            read.Update("new", null, null, null, null, null, null, _backups.Id, null, _reduce.Id, null,
                new DatabasesBackupFilesExchange(null, ".upload", _exchange.Id, null, _keep.Id));
            new GlobalSettingsRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        GlobalSettings stored = await Stored();
        Assert.Equal(GlobalSettingsId.Singleton, stored.Id);
        Assert.Equal("new", stored.ServiceDescriptionSignature);
        Assert.Null(stored.MediatRLicenseKey);
        Assert.Equal(_backups.Id, stored.FileStorageForExchangeId);
        Assert.Null(stored.SmartSchemaForExchangeId);
        Assert.Equal(_reduce.Id, stored.SmartSchemaForLocalId);
        Assert.Null(stored.LocalPackageManagerWebApiClientId);
        Assert.Null(stored.DatabasesBackupFilesExchange.DownloadTempExtension);
        Assert.Equal(".upload", stored.DatabasesBackupFilesExchange.UploadTempExtension);
        Assert.Equal(_exchange.Id, stored.DatabasesBackupFilesExchange.ExchangeFileStorageId);
        Assert.Null(stored.DatabasesBackupFilesExchange.ExchangeSmartSchemaId);
        Assert.Equal(_keep.Id, stored.DatabasesBackupFilesExchange.LocalSmartSchemaId);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token of the whole row: the update of a stale read writes nothing, the exchange
    //parameters included
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheSingletonChangedAfterItWasRead()
    {
        await Store(NewWithEveryReference());
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        GlobalSettings readFirst = await Read(first);
        GlobalSettings readSecond = await Read(second);
        readFirst.Update("first", null, null, null, null, null, null, null, null, null, null,
            new DatabasesBackupFilesExchange(".first", null, null, null, null));
        new GlobalSettingsRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("second", null, null, null, null, null, null, null, null, null, null,
            new DatabasesBackupFilesExchange(".second", null, null, null, null));
        new GlobalSettingsRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        GlobalSettings stored = await Stored();
        Assert.Equal("first", stored.ServiceDescriptionSignature);
        Assert.Equal(".first", stored.DatabasesBackupFilesExchange.DownloadTempExtension);
        Assert.Equal(2, stored.Version);
    }

    //A referenced record cannot be deleted while the settings use it, through the exchange parameters as well
    [Fact]
    public async Task DeletingAReferencedRecord_IsRefusedOnSave()
    {
        await Store(TestData.NewGlobalSettings(exchangeFileStorage: _backups));
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.FileStorages.Remove(await context.FileStorages.SingleAsync(x => x.Name == "Backups"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
