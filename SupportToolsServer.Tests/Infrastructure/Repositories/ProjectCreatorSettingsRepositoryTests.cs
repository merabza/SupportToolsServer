using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class ProjectCreatorSettingsRepositoryTests : IAsyncLifetime
{
    private readonly FileStorage _backups = TestData.NewFileStorage("Backups");
    private readonly DatabaseServerConnection _connection = TestData.NewDatabaseServerConnection("Pazisi");
    private readonly DeploymentEnvironment _prod = TestData.NewEnvironment("Prod");
    private readonly SmartSchema _reduce = TestData.NewSmartSchema("Reduce");
    private readonly Server _server = TestData.NewServer("dl360");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.Servers.Add(_server);
        context.Environments.Add(_prod);
        context.DatabaseServerConnections.Add(_connection);
        context.FileStorages.Add(_backups);
        context.SmartSchemas.Add(_reduce);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private async Task Store(ProjectCreatorSettings projectCreatorSettings)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ProjectCreatorSettingsRepository(context).Add(projectCreatorSettings);
        await context.SaveChangesAsync();
    }

    private static async Task<ProjectCreatorSettings> Read(SupportToolsServerDbContext context)
    {
        ProjectCreatorSettings? projectCreatorSettings =
            await new ProjectCreatorSettingsRepository(context).Get(CancellationToken.None);
        return Assert.IsType<ProjectCreatorSettings>(projectCreatorSettings);
    }

    private async Task<ProjectCreatorSettings> Stored()
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.ProjectCreatorSettings.AsNoTracking().SingleAsync();
    }

    [Fact]
    public async Task Get_ReturnsNull_BeforeTheSingletonIsCreated()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new ProjectCreatorSettingsRepository(context).Get(CancellationToken.None));
    }

    [Fact]
    public async Task Get_ReadsTheSingletonWithEveryValueWithoutTrackingIt()
    {
        await Store(ProjectCreatorSettings.Create(4, "FakeHost", @"D:\1WorkDotnet", @"D:\1WorkSecurity", _server.Id,
            _prod.Id, _connection.Id, _backups.Id, _reduce.Id));
        await using SupportToolsServerDbContext context = _database.NewContext();

        ProjectCreatorSettings read = await Read(context);

        Assert.Equal(ProjectCreatorSettingsId.Singleton, read.Id);
        Assert.Equal(4, read.IndentSize);
        Assert.Equal("FakeHost", read.FakeHostProjectName);
        Assert.Equal(@"D:\1WorkDotnet", read.ProjectsFolderPathReal);
        Assert.Equal(@"D:\1WorkSecurity", read.SecretsFolderPathReal);
        Assert.Equal(_server.Id, read.ProductionServerId);
        Assert.Equal(_prod.Id, read.ProductionEnvironmentId);
        Assert.Equal(_connection.Id, read.DeveloperDbConnectionId);
        Assert.Equal(_backups.Id, read.DatabaseExchangeFileStorageId);
        Assert.Equal(_reduce.Id, read.UseSmartSchemaId);
        Assert.Equal(1, read.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //The fixed key allows one record: a second first create fails on the primary key
    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheSingletonIsAlreadyStored()
    {
        await Store(TestData.NewProjectCreatorSettings());
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ProjectCreatorSettingsRepository(context).Add(TestData.NewProjectCreatorSettings(_server));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The check constraint refuses any other key, even in the empty table
    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheKeyIsNotTheFixedOne()
    {
        var other = new ProjectCreatorSettings(new ProjectCreatorSettingsId(Guid.NewGuid()), 4, null, null, null,
            null, null, null, null, null, 1);
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ProjectCreatorSettingsRepository(context).Add(other);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.False(await check.ProjectCreatorSettings.AnyAsync());
    }

    //Each foreign key refuses a referenced record that is not stored
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Add_IsRefusedOnSave_WhenAReferencedRecordIsNotStored(int missingReference)
    {
        ProjectCreatorSettings projectCreatorSettings = TestData.NewProjectCreatorSettings(
            missingReference == 0 ? TestData.NewServer("guria") : _server,
            missingReference == 1 ? TestData.NewEnvironment("Stage") : _prod,
            missingReference == 2 ? TestData.NewDatabaseServerConnection("Pc9.Sql") : _connection,
            missingReference == 3 ? TestData.NewFileStorage("Missing") : _backups,
            missingReference == 4 ? TestData.NewSmartSchema("Hourly") : _reduce);
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ProjectCreatorSettingsRepository(context).Add(projectCreatorSettings);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadSingletonWithItsNewVersion()
    {
        await Store(TestData.NewProjectCreatorSettings(_server, _prod, _connection, _backups, _reduce));
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            ProjectCreatorSettings read = await Read(context);
            read.Update(2, "Host", @"D:\Projects", null, null, _prod.Id, null, _backups.Id, null);
            new ProjectCreatorSettingsRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        ProjectCreatorSettings stored = await Stored();
        Assert.Equal(ProjectCreatorSettingsId.Singleton, stored.Id);
        Assert.Equal(2, stored.IndentSize);
        Assert.Equal("Host", stored.FakeHostProjectName);
        Assert.Equal(@"D:\Projects", stored.ProjectsFolderPathReal);
        Assert.Null(stored.SecretsFolderPathReal);
        Assert.Null(stored.ProductionServerId);
        Assert.Equal(_prod.Id, stored.ProductionEnvironmentId);
        Assert.Null(stored.DeveloperDbConnectionId);
        Assert.Equal(_backups.Id, stored.DatabaseExchangeFileStorageId);
        Assert.Null(stored.UseSmartSchemaId);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheSingletonChangedAfterItWasRead()
    {
        await Store(TestData.NewProjectCreatorSettings());
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        ProjectCreatorSettings readFirst = await Read(first);
        ProjectCreatorSettings readSecond = await Read(second);
        readFirst.Update(2, "first", null, null, null, null, null, null, null);
        new ProjectCreatorSettingsRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update(8, "second", null, null, null, null, null, null, null);
        new ProjectCreatorSettingsRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        ProjectCreatorSettings stored = await Stored();
        Assert.Equal("first", stored.FakeHostProjectName);
        Assert.Equal(2, stored.Version);
    }

    //A referenced record cannot be deleted while the settings use it
    [Fact]
    public async Task DeletingAReferencedRecord_IsRefusedOnSave()
    {
        await Store(TestData.NewProjectCreatorSettings(_server));
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.Servers.Remove(await context.Servers.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
