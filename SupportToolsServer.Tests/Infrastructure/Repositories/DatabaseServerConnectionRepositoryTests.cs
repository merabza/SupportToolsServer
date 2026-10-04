using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class DatabaseServerConnectionRepositoryTests : IAsyncLifetime
{
    private readonly ApiClient _webAgent = TestData.NewApiClient("Pc1.WebAgent");
    private SupportToolsServerSqliteDatabase _database = null!;
    private DatabaseServerConnection _pc1 = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        _pc1 = TestData.NewDatabaseServerConnection("Pc1.Sql", _webAgent, ["Default", "Second"]);
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.ApiClients.Add(_webAgent);
        context.DatabaseServerConnections.AddRange(_pc1,
            TestData.NewDatabaseServerConnection("Pc2.Sql", foldersSetNames: ["Default"]));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<DatabaseServerConnection> Read(SupportToolsServerDbContext context, string name)
    {
        DatabaseServerConnection? connection =
            await new DatabaseServerConnectionRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<DatabaseServerConnection>(connection);
    }

    private static async Task<DatabaseServerConnection> ReadForUpdate(SupportToolsServerDbContext context,
        string name)
    {
        DatabaseServerConnection? connection =
            await new DatabaseServerConnectionRepository(context).GetByNameForUpdate(name, CancellationToken.None);
        return Assert.IsType<DatabaseServerConnection>(connection);
    }

    private async Task<DatabaseServerConnection?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.DatabaseServerConnections.AsNoTracking().Include(x => x.DatabaseFoldersSets)
            .SingleOrDefaultAsync(x => x.Name == name);
    }

    //Every folders set row of the database, as "name:backup", so that orphans would show
    private async Task<List<string>> StoredFoldersSetRows()
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.Set<DatabaseFoldersSet>().AsNoTracking().OrderBy(x => x.Backup)
            .Select(x => x.Name + ":" + x.Backup).ToListAsync();
    }

    private static List<string> FoldersSetsOf(DatabaseServerConnection connection)
    {
        return [.. connection.DatabaseFoldersSets.Select(x => x.Name).Order()];
    }

    [Fact]
    public async Task GetAll_ReturnsEveryConnectionWithItsFoldersSetsWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<DatabaseServerConnection> all =
            await new DatabaseServerConnectionRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["Pc1.Sql", "Pc2.Sql"], all.Select(x => x.Name).Order());
        Assert.Equal(["Default", "Second"], FoldersSetsOf(all.Single(x => x.Name == "Pc1.Sql")));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("Pc1.Sql")]
    [InlineData("pc1.sql")]
    [InlineData("PC1.SQL")]
    public async Task GetByName_FindsTheNameWithoutCaseWithEveryValueAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        DatabaseServerConnection found = await Read(context, name);

        Assert.Equal(_pc1.Id, found.Id);
        Assert.Equal("Pc1.Sql", found.Name);
        Assert.Equal("SqlServer", found.DatabaseServerProvider);
        Assert.Equal(_webAgent.Id, found.DbWebAgentId);
        Assert.Equal("Main", found.RemoteDbConnectionName);
        Assert.Equal("pc1", found.ServerAddress);
        Assert.True(found.WindowsNtIntegratedSecurity);
        Assert.Equal(TestData.MadeUpUser, found.ServerUser);
        Assert.Equal(TestData.MadeUpPassword, found.ServerPass);
        Assert.True(found.TrustServerCertificate);
        Assert.Equal(30, found.ConnectionTimeOut);
        Assert.True(found.Encrypt);
        Assert.Equal(["Default", "Second"], FoldersSetsOf(found));
        DatabaseFoldersSet foldersSet = found.DatabaseFoldersSets.Single(x => x.Name == "Default");
        Assert.Equal(@"D:\Default\Bak", foldersSet.Backup);
        Assert.Equal(@"D:\Default\Data", foldersSet.Data);
        Assert.Equal(@"D:\Default\Log", foldersSet.DataLog);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new DatabaseServerConnectionRepository(context).GetByName("Pc3.Sql", CancellationToken.None));
    }

    //Only the connection that is updated and its folders sets are tracked
    [Theory]
    [InlineData("Pc1.Sql")]
    [InlineData("PC1.SQL")]
    public async Task GetByNameForUpdate_FindsTheNameWithoutCaseAndTracksOnlyThatConnectionWithItsFoldersSets(
        string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        DatabaseServerConnection found = await ReadForUpdate(context, name);

        Assert.Equal(_pc1.Id, found.Id);
        Assert.Equal(["Default", "Second"], FoldersSetsOf(found));
        Assert.Equal(3, context.ChangeTracker.Entries().Count());
        Assert.Equal(EntityState.Unchanged, context.Entry(found).State);
    }

    [Fact]
    public async Task GetByNameForUpdate_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new DatabaseServerConnectionRepository(context).GetByNameForUpdate("Pc3.Sql",
            CancellationToken.None));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Add_StoresTheConnectionWithItsFoldersSetsAndTheFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new DatabaseServerConnectionRepository(context).Add(DatabaseServerConnection.Create("Pc3.Sql",
                "WebAgent", _webAgent.Id, null, null, false, null, null, false, 15, false,
                [DatabaseFoldersSet.Create("Default", @"E:\Bak", null, null)]));
            await context.SaveChangesAsync();
        }

        DatabaseServerConnection? stored = await Stored("Pc3.Sql");
        Assert.NotNull(stored);
        Assert.Equal("WebAgent", stored.DatabaseServerProvider);
        Assert.Equal(_webAgent.Id, stored.DbWebAgentId);
        Assert.Null(stored.ServerUser);
        Assert.Null(stored.ServerPass);
        DatabaseFoldersSet foldersSet = Assert.Single(stored.DatabaseFoldersSets);
        Assert.Equal(@"E:\Bak", foldersSet.Backup);
        Assert.Null(foldersSet.Data);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new DatabaseServerConnectionRepository(context).Add(TestData.NewDatabaseServerConnection("Pc1.Sql"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The foreign key refuses a web agent that is not stored
    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheWebAgentIsNotStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new DatabaseServerConnectionRepository(context).Add(
            TestData.NewDatabaseServerConnection("Pc3.Sql", TestData.NewApiClient("Pc3.WebAgent")));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The unique index of the folders sets keeps one name per connection
    [Fact]
    public async Task Add_IsRefusedOnSave_WhenAFoldersSetNameRepeatsInTheConnection()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new DatabaseServerConnectionRepository(context).Add(
            TestData.NewDatabaseServerConnection("Pc3.Sql", foldersSetNames: ["Default", "Default"]));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The update replaces the folders sets: the replaced rows are deleted and no orphan is left, even for a folders set
    //of the same name, whose old row goes before the new one is inserted
    [Fact]
    public async Task Update_ReplacesTheFoldersSetsOfTheReadConnectionAndStoresItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            DatabaseServerConnection read = await ReadForUpdate(context, "Pc1.Sql");
            read.Update("PC1.SQL", "SqlServer", null, null, "pc9", false, null, null, false, 60, false,
            [
                DatabaseFoldersSet.Create("Default", @"E:\NewBak", null, null),
                DatabaseFoldersSet.Create("Third", @"F:\Bak", null, null)
            ]);
            new DatabaseServerConnectionRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        DatabaseServerConnection? stored = await Stored("PC1.SQL");
        Assert.NotNull(stored);
        Assert.Equal(_pc1.Id, stored.Id);
        Assert.Null(stored.DbWebAgentId);
        Assert.Equal("pc9", stored.ServerAddress);
        Assert.Null(stored.ServerPass);
        Assert.Equal(60, stored.ConnectionTimeOut);
        Assert.Equal(["Default", "Third"], FoldersSetsOf(stored));
        Assert.Equal(2, stored.Version);
        Assert.Equal([@"Default:D:\Default\Bak", @"Default:E:\NewBak", @"Third:F:\Bak"],
            await StoredFoldersSetRows());
    }

    //The version is the concurrency token: the update of a stale read writes nothing, neither the connection nor its
    //folders sets
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheConnectionChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        DatabaseServerConnection readFirst = await ReadForUpdate(first, "Pc1.Sql");
        DatabaseServerConnection readSecond = await ReadForUpdate(second, "Pc1.Sql");
        readFirst.Update("Pc1.Sql", "SqlServer", null, null, "first", false, null, null, false, 0, false,
            [DatabaseFoldersSet.Create("First", @"G:\Bak", null, null)]);
        new DatabaseServerConnectionRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("Pc1.Sql", "SqlServer", null, null, "second", false, null, null, false, 0, false,
            [DatabaseFoldersSet.Create("Second", @"H:\Bak", null, null)]);
        new DatabaseServerConnectionRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        DatabaseServerConnection? stored = await Stored("Pc1.Sql");
        Assert.NotNull(stored);
        Assert.Equal("first", stored.ServerAddress);
        Assert.Equal(["First"], FoldersSetsOf(stored));
        Assert.Equal(2, stored.Version);
        Assert.Equal([@"Default:D:\Default\Bak", @"First:G:\Bak"], await StoredFoldersSetRows());
    }

    //Update relies on the one version increment of DatabaseServerConnection.Update: it expects the stored version to
    //be one less than the version of the instance, so an instance whose version did not grow is refused
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheVersionOfTheReadConnectionDidNotGrow()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        DatabaseServerConnection read = await ReadForUpdate(context, "Pc1.Sql");
        new DatabaseServerConnectionRepository(context).Update(read);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());

        DatabaseServerConnection? stored = await Stored("Pc1.Sql");
        Assert.NotNull(stored);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheConnectionWithItsFoldersSetsOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new DatabaseServerConnectionRepository(context).Delete(await Read(context, "Pc1.Sql"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["Pc2.Sql"], await check.DatabaseServerConnections.Select(x => x.Name).ToListAsync());
        Assert.Equal([@"Default:D:\Default\Bak"], await StoredFoldersSetRows());
        Assert.Equal(1, await check.ApiClients.CountAsync());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheConnectionChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        DatabaseServerConnection readFirst = await ReadForUpdate(first, "Pc1.Sql");
        DatabaseServerConnection readSecond = await Read(second, "Pc1.Sql");
        readFirst.Update("Pc1.Sql", "SqlServer", null, null, "first", false, null, null, false, 0, false, []);
        new DatabaseServerConnectionRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new DatabaseServerConnectionRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("Pc1.Sql"));
    }
}
