using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class NpmPackageRepositoryTests : IAsyncLifetime
{
    private readonly NpmPackage _npmPackage = TestData.NewNpmPackage("react", "UI library");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.NpmPackages.AddRange(_npmPackage, TestData.NewNpmPackage("@reduxjs/toolkit"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<NpmPackage> Read(SupportToolsServerDbContext context, string name)
    {
        NpmPackage? npmPackage = await new NpmPackageRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<NpmPackage>(npmPackage);
    }

    private async Task<NpmPackage?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.NpmPackages.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredNpmPackageWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<NpmPackage> all = await new NpmPackageRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["@reduxjs/toolkit", "react"], all.Select(x => x.Name).Order());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("react")]
    [InlineData("REACT")]
    [InlineData("React")]
    public async Task GetByName_FindsTheNameWithoutCaseAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        NpmPackage found = await Read(context, name);

        Assert.Equal(_npmPackage.Id, found.Id);
        Assert.Equal("react", found.Name);
        Assert.Equal("UI library", found.Description);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        NpmPackage? found = await new NpmPackageRepository(context).GetByName("left-pad", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Add_StoresTheNpmPackageWithItsFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new NpmPackageRepository(context).Add(NpmPackage.Create("left-pad", null));
            await context.SaveChangesAsync();
        }

        NpmPackage? stored = await Stored("left-pad");
        Assert.NotNull(stored);
        Assert.Null(stored.Description);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new NpmPackageRepository(context).Add(NpmPackage.Create("react", "Other"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadNpmPackageWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            NpmPackage read = await Read(context, "react");
            read.Update("REACT", "React UI");
            new NpmPackageRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        NpmPackage? stored = await Stored("REACT");
        Assert.NotNull(stored);
        Assert.Equal(_npmPackage.Id, stored.Id);
        Assert.Equal("React UI", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheNpmPackageChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        NpmPackage readFirst = await Read(first, "react");
        NpmPackage readSecond = await Read(second, "react");
        readFirst.Update("react", "First");
        new NpmPackageRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("react", "Second");
        new NpmPackageRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        NpmPackage? stored = await Stored("react");
        Assert.NotNull(stored);
        Assert.Equal("First", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheNpmPackageOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new NpmPackageRepository(context).Delete(await Read(context, "react"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["@reduxjs/toolkit"], await check.NpmPackages.Select(x => x.Name).ToListAsync());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheNpmPackageChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        NpmPackage readFirst = await Read(first, "react");
        NpmPackage readSecond = await Read(second, "react");
        readFirst.Update("react", "First");
        new NpmPackageRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new NpmPackageRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("react"));
    }
}
