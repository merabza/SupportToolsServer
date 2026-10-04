using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class ApiClientRepositoryTests : IAsyncLifetime
{
    private readonly ApiClient _pc1 = TestData.NewApiClient("Pc1.WebAgent");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.ApiClients.AddRange(_pc1, TestData.NewApiClient("Pc2.WebAgent", null, null));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<ApiClient> Read(SupportToolsServerDbContext context, string name)
    {
        ApiClient? apiClient = await new ApiClientRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<ApiClient>(apiClient);
    }

    private async Task<ApiClient?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.ApiClients.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredApiClientWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<ApiClient> all = await new ApiClientRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["Pc1.WebAgent", "Pc2.WebAgent"], all.Select(x => x.Name).Order());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("Pc1.WebAgent")]
    [InlineData("pc1.webagent")]
    [InlineData("PC1.WEBAGENT")]
    public async Task GetByName_FindsTheNameWithoutCaseAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        ApiClient found = await Read(context, name);

        Assert.Equal(_pc1.Id, found.Id);
        Assert.Equal("Pc1.WebAgent", found.Name);
        Assert.Equal("http://localhost:5031/api/v1/", found.Server);
        Assert.Equal(TestData.MadeUpApiKey, found.ApiKey);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new ApiClientRepository(context).GetByName("Pc3.WebAgent", CancellationToken.None));
    }

    [Fact]
    public async Task Add_StoresTheApiClientWithItsFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ApiClientRepository(context).Add(ApiClient.Create("Pc3.WebAgent", null, "key-3"));
            await context.SaveChangesAsync();
        }

        ApiClient? stored = await Stored("Pc3.WebAgent");
        Assert.NotNull(stored);
        Assert.Null(stored.Server);
        Assert.Equal("key-3", stored.ApiKey);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ApiClientRepository(context).Add(ApiClient.Create("Pc1.WebAgent", null, null));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadApiClientWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            ApiClient read = await Read(context, "Pc1.WebAgent");
            read.Update("PC1.WEBAGENT", "https://pc1.example.com/api/v1/", "key-new");
            new ApiClientRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        ApiClient? stored = await Stored("PC1.WEBAGENT");
        Assert.NotNull(stored);
        Assert.Equal(_pc1.Id, stored.Id);
        Assert.Equal("https://pc1.example.com/api/v1/", stored.Server);
        Assert.Equal("key-new", stored.ApiKey);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheApiClientChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        ApiClient readFirst = await Read(first, "Pc1.WebAgent");
        ApiClient readSecond = await Read(second, "Pc1.WebAgent");
        readFirst.Update("Pc1.WebAgent", "http://first/", "key-first");
        new ApiClientRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("Pc1.WebAgent", "http://second/", "key-second");
        new ApiClientRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        ApiClient? stored = await Stored("Pc1.WebAgent");
        Assert.NotNull(stored);
        Assert.Equal("http://first/", stored.Server);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheApiClientOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ApiClientRepository(context).Delete(await Read(context, "Pc1.WebAgent"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["Pc2.WebAgent"], await check.ApiClients.Select(x => x.Name).ToListAsync());
    }

    //The foreign key of the connections is Restrict: the database keeps an ApiClient that a connection uses, even if
    //the handler's check missed it
    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenAConnectionUsesTheApiClient()
    {
        await using (SupportToolsServerDbContext setup = _database.NewContext())
        {
            setup.DatabaseServerConnections.Add(TestData.NewDatabaseServerConnection("Pc1.Sql", _pc1));
            await setup.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext context = _database.NewContext();
        new ApiClientRepository(context).Delete(await Read(context, "Pc1.WebAgent"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.NotNull(await Stored("Pc1.WebAgent"));
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheApiClientChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        ApiClient readFirst = await Read(first, "Pc1.WebAgent");
        ApiClient readSecond = await Read(second, "Pc1.WebAgent");
        readFirst.Update("Pc1.WebAgent", "http://first/", "key-first");
        new ApiClientRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new ApiClientRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("Pc1.WebAgent"));
    }
}
