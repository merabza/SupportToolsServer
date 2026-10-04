using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class RuntimeRepositoryTests : IAsyncLifetime
{
    private readonly Runtime _runtime = TestData.NewRuntime("win-x64", "Windows x64");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.Runtimes.AddRange(_runtime, TestData.NewRuntime("linux-x64"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<Runtime> Read(SupportToolsServerDbContext context, string name)
    {
        Runtime? runtime = await new RuntimeRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<Runtime>(runtime);
    }

    private async Task<Runtime?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.Runtimes.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredRuntimeWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<Runtime> all = await new RuntimeRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["linux-x64", "win-x64"], all.Select(x => x.Name).Order());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("win-x64")]
    [InlineData("WIN-X64")]
    [InlineData("Win-X64")]
    public async Task GetByName_FindsTheNameWithoutCaseAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Runtime found = await Read(context, name);

        Assert.Equal(_runtime.Id, found.Id);
        Assert.Equal("win-x64", found.Name);
        Assert.Equal("Windows x64", found.Description);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Runtime? found = await new RuntimeRepository(context).GetByName("osx-arm64", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Add_StoresTheRuntimeWithItsFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new RuntimeRepository(context).Add(Runtime.Create("osx-arm64", null));
            await context.SaveChangesAsync();
        }

        Runtime? stored = await Stored("osx-arm64");
        Assert.NotNull(stored);
        Assert.Null(stored.Description);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new RuntimeRepository(context).Add(Runtime.Create("win-x64", "Other"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadRuntimeWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            Runtime read = await Read(context, "win-x64");
            read.Update("WIN-X64", "Windows 64 bit");
            new RuntimeRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        Runtime? stored = await Stored("WIN-X64");
        Assert.NotNull(stored);
        Assert.Equal(_runtime.Id, stored.Id);
        Assert.Equal("Windows 64 bit", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheRuntimeChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        Runtime readFirst = await Read(first, "win-x64");
        Runtime readSecond = await Read(second, "win-x64");
        readFirst.Update("win-x64", "First");
        new RuntimeRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("win-x64", "Second");
        new RuntimeRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Runtime? stored = await Stored("win-x64");
        Assert.NotNull(stored);
        Assert.Equal("First", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheRuntimeOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new RuntimeRepository(context).Delete(await Read(context, "win-x64"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["linux-x64"], await check.Runtimes.Select(x => x.Name).ToListAsync());
    }

    //The foreign key of the servers is Restrict: the database keeps a Runtime that a server uses, even if the handler's
    //check missed it
    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenAServerUsesTheRuntime()
    {
        await using (SupportToolsServerDbContext setup = _database.NewContext())
        {
            setup.Servers.Add(TestData.NewServer("PAZISI", runtime: _runtime));
            await setup.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext context = _database.NewContext();
        new RuntimeRepository(context).Delete(await Read(context, "win-x64"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.NotNull(await Stored("win-x64"));
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheRuntimeChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        Runtime readFirst = await Read(first, "win-x64");
        Runtime readSecond = await Read(second, "win-x64");
        readFirst.Update("win-x64", "First");
        new RuntimeRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new RuntimeRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("win-x64"));
    }
}
