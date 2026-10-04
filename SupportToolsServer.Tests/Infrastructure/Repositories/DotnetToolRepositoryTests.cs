using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DotnetTools;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class DotnetToolRepositoryTests : IAsyncLifetime
{
    private readonly DotnetTool _dotnetTool =
        TestData.NewDotnetTool("DotnetEf", "dotnet-ef", "9.0.8", "Entity Framework");

    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.DotnetTools.AddRange(_dotnetTool, TestData.NewDotnetTool("Stryker", "dotnet-stryker"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<DotnetTool> Read(SupportToolsServerDbContext context, string name)
    {
        DotnetTool? dotnetTool = await new DotnetToolRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<DotnetTool>(dotnetTool);
    }

    private async Task<DotnetTool?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.DotnetTools.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredDotnetToolWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<DotnetTool> all = await new DotnetToolRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["DotnetEf", "Stryker"], all.Select(x => x.Name).Order());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("DotnetEf")]
    [InlineData("DOTNETEF")]
    [InlineData("dotnetef")]
    public async Task GetByName_FindsTheNameWithoutCaseAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        DotnetTool found = await Read(context, name);

        Assert.Equal(_dotnetTool.Id, found.Id);
        Assert.Equal("DotnetEf", found.Name);
        Assert.Equal("dotnet-ef", found.PackageId);
        Assert.Equal("9.0.8", found.MaxVersion);
        Assert.Equal("Entity Framework", found.Description);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        DotnetTool? found =
            await new DotnetToolRepository(context).GetByName("ReportGenerator", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Add_StoresTheDotnetToolWithItsFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new DotnetToolRepository(context).Add(DotnetTool.Create("ReportGenerator",
                "dotnet-reportgenerator-globaltool", null, null));
            await context.SaveChangesAsync();
        }

        DotnetTool? stored = await Stored("ReportGenerator");
        Assert.NotNull(stored);
        Assert.Equal("dotnet-reportgenerator-globaltool", stored.PackageId);
        Assert.Null(stored.MaxVersion);
        Assert.Null(stored.Description);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new DotnetToolRepository(context).Add(DotnetTool.Create("DotnetEf", "dotnet-ef", null, "Other"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadDotnetToolWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            DotnetTool read = await Read(context, "DotnetEf");
            read.Update("DOTNETEF", "Dotnet-Ef", "10.0.12", "EF Core tools");
            new DotnetToolRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        DotnetTool? stored = await Stored("DOTNETEF");
        Assert.NotNull(stored);
        Assert.Equal(_dotnetTool.Id, stored.Id);
        Assert.Equal("Dotnet-Ef", stored.PackageId);
        Assert.Equal("10.0.12", stored.MaxVersion);
        Assert.Equal("EF Core tools", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheDotnetToolChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        DotnetTool readFirst = await Read(first, "DotnetEf");
        DotnetTool readSecond = await Read(second, "DotnetEf");
        readFirst.Update("DotnetEf", "dotnet-ef", null, "First");
        new DotnetToolRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("DotnetEf", "dotnet-ef", null, "Second");
        new DotnetToolRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        DotnetTool? stored = await Stored("DotnetEf");
        Assert.NotNull(stored);
        Assert.Equal("First", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheDotnetToolOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new DotnetToolRepository(context).Delete(await Read(context, "DotnetEf"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["Stryker"], await check.DotnetTools.Select(x => x.Name).ToListAsync());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheDotnetToolChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        DotnetTool readFirst = await Read(first, "DotnetEf");
        DotnetTool readSecond = await Read(second, "DotnetEf");
        readFirst.Update("DotnetEf", "dotnet-ef", null, "First");
        new DotnetToolRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new DotnetToolRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("DotnetEf"));
    }
}
