using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class ServerRepositoryTests : IAsyncLifetime
{
    private readonly Runtime _runtime = TestData.NewRuntime("linux-x64");
    private readonly ApiClient _webAgent = TestData.NewApiClient("Dl360.WebAgent");
    private readonly ApiClient _webAgentInstaller = TestData.NewApiClient("Dl360.Installer");
    private SupportToolsServerSqliteDatabase _database = null!;
    private Server _dl360 = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        _dl360 = TestData.NewServer("dl360", _webAgent, _webAgentInstaller, _runtime);
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.ApiClients.AddRange(_webAgent, _webAgentInstaller);
        context.Runtimes.Add(_runtime);
        context.Servers.AddRange(_dl360, TestData.NewServer("PAZISI"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<Server> Read(SupportToolsServerDbContext context, string name)
    {
        Server? server = await new ServerRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<Server>(server);
    }

    private async Task<Server?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.Servers.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredServerWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<Server> all = await new ServerRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["dl360", "PAZISI"], all.Select(x => x.Name).Order(StringComparer.OrdinalIgnoreCase));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("dl360")]
    [InlineData("DL360")]
    [InlineData("Dl360")]
    public async Task GetByName_FindsTheNameWithoutCaseWithEveryValueAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Server found = await Read(context, name);

        Assert.Equal(_dl360.Id, found.Id);
        Assert.Equal("dl360", found.Name);
        Assert.Equal(_webAgent.Id, found.WebAgentId);
        Assert.Equal(_webAgentInstaller.Id, found.WebAgentInstallerId);
        Assert.Equal("deployer", found.FilesUserName);
        Assert.Equal("deployers", found.FilesUsersGroupName);
        Assert.Equal(_runtime.Id, found.RuntimeId);
        Assert.Equal("/home/deployer/Download", found.ServerSideDownloadFolder);
        Assert.Equal("/opt/apps", found.ServerSideDeployFolder);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new ServerRepository(context).GetByName("guria", CancellationToken.None));
    }

    [Fact]
    public async Task Add_StoresTheServerWithItsFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ServerRepository(context).Add(Server.Create("guria", null, _webAgent.Id, null, null, _runtime.Id,
                null, @"D:\Apps"));
            await context.SaveChangesAsync();
        }

        Server? stored = await Stored("guria");
        Assert.NotNull(stored);
        Assert.Null(stored.WebAgentId);
        Assert.Equal(_webAgent.Id, stored.WebAgentInstallerId);
        Assert.Null(stored.FilesUserName);
        Assert.Equal(_runtime.Id, stored.RuntimeId);
        Assert.Equal(@"D:\Apps", stored.ServerSideDeployFolder);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ServerRepository(context).Add(TestData.NewServer("dl360"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //Each foreign key refuses a referenced record that is not stored
    [Theory]
    [InlineData(nameof(Server.WebAgentId))]
    [InlineData(nameof(Server.WebAgentInstallerId))]
    [InlineData(nameof(Server.RuntimeId))]
    public async Task Add_IsRefusedOnSave_WhenAReferencedRecordIsNotStored(string missingReference)
    {
        ApiClient missingApiClient = TestData.NewApiClient("Pc9.WebAgent");
        Server server = missingReference switch
        {
            nameof(Server.WebAgentId) => TestData.NewServer("guria", missingApiClient, _webAgentInstaller, _runtime),
            nameof(Server.WebAgentInstallerId) => TestData.NewServer("guria", _webAgent, missingApiClient, _runtime),
            _ => TestData.NewServer("guria", _webAgent, _webAgentInstaller, TestData.NewRuntime("osx-arm64"))
        };
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ServerRepository(context).Add(server);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadServerWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            Server read = await Read(context, "dl360");
            read.Update("DL360", _webAgentInstaller.Id, null, "admin", null, null, null, "/srv/apps");
            new ServerRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        Server? stored = await Stored("DL360");
        Assert.NotNull(stored);
        Assert.Equal(_dl360.Id, stored.Id);
        Assert.Equal(_webAgentInstaller.Id, stored.WebAgentId);
        Assert.Null(stored.WebAgentInstallerId);
        Assert.Equal("admin", stored.FilesUserName);
        Assert.Null(stored.RuntimeId);
        Assert.Equal("/srv/apps", stored.ServerSideDeployFolder);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheServerChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        Server readFirst = await Read(first, "dl360");
        Server readSecond = await Read(second, "dl360");
        readFirst.Update("dl360", null, null, "first", null, null, null, null);
        new ServerRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("dl360", null, null, "second", null, null, null, null);
        new ServerRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Server? stored = await Stored("dl360");
        Assert.NotNull(stored);
        Assert.Equal("first", stored.FilesUserName);
        Assert.Equal(2, stored.Version);
    }

    //The referenced records belong to other aggregates and stay
    [Fact]
    public async Task Delete_RemovesTheServerOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ServerRepository(context).Delete(await Read(context, "dl360"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["PAZISI"], await check.Servers.Select(x => x.Name).ToListAsync());
        Assert.Equal(2, await check.ApiClients.CountAsync());
        Assert.Equal(1, await check.Runtimes.CountAsync());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheServerChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        Server readFirst = await Read(first, "dl360");
        Server readSecond = await Read(second, "dl360");
        readFirst.Update("dl360", null, null, "first", null, null, null, null);
        new ServerRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new ServerRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("dl360"));
    }
}
