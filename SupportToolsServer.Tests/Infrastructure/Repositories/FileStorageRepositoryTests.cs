using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class FileStorageRepositoryTests : IAsyncLifetime
{
    private readonly FileStorage _exchange = TestData.NewFileStorage("Exchange");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.FileStorages.AddRange(_exchange, TestData.NewFileStorage("LocalBak", @"D:\Bak", null, null));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<FileStorage> Read(SupportToolsServerDbContext context, string name)
    {
        FileStorage? fileStorage = await new FileStorageRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<FileStorage>(fileStorage);
    }

    private async Task<FileStorage?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.FileStorages.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredFileStorageWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<FileStorage> all = await new FileStorageRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["Exchange", "LocalBak"], all.Select(x => x.Name).Order());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("Exchange")]
    [InlineData("exchange")]
    [InlineData("EXCHANGE")]
    public async Task GetByName_FindsTheNameWithoutCaseAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        FileStorage found = await Read(context, name);

        Assert.Equal(_exchange.Id, found.Id);
        Assert.Equal("Exchange", found.Name);
        Assert.Equal("ftp://ftp.example.com/x/", found.FileStoragePath);
        Assert.Equal(TestData.MadeUpUser, found.UserName);
        Assert.Equal(TestData.MadeUpPassword, found.Password);
        //Assert.Equal(255, found.FileNameMaxLength);
        //Assert.Equal(4, found.FileSizeSplitPositionInRow);
        Assert.Equal(1, found.FtpSiteLsFileOffset);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new FileStorageRepository(context).GetByName("Archive", CancellationToken.None));
    }

    [Fact]
    public async Task Add_StoresTheFileStorageWithItsFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new FileStorageRepository(context).Add(FileStorage.Create("Archive", @"E:\Archive", null, null, 
                //0, 0, 
                0));
            await context.SaveChangesAsync();
        }

        FileStorage? stored = await Stored("Archive");
        Assert.NotNull(stored);
        Assert.Equal(@"E:\Archive", stored.FileStoragePath);
        Assert.Null(stored.UserName);
        Assert.Null(stored.Password);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new FileStorageRepository(context).Add(FileStorage.Create("Exchange", null, null, null, 
            //0, 0, 
            0));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadFileStorageWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            FileStorage read = await Read(context, "Exchange");
            read.Update("EXCHANGE", "ftp://ftp.example.com/y/", "user-y", "password-y", 
                //100, 5, 
                2);
            new FileStorageRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        FileStorage? stored = await Stored("EXCHANGE");
        Assert.NotNull(stored);
        Assert.Equal(_exchange.Id, stored.Id);
        Assert.Equal("ftp://ftp.example.com/y/", stored.FileStoragePath);
        Assert.Equal("user-y", stored.UserName);
        Assert.Equal("password-y", stored.Password);
        //Assert.Equal(100, stored.FileNameMaxLength);
        //Assert.Equal(5, stored.FileSizeSplitPositionInRow);
        Assert.Equal(2, stored.FtpSiteLsFileOffset);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheFileStorageChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        FileStorage readFirst = await Read(first, "Exchange");
        FileStorage readSecond = await Read(second, "Exchange");
        readFirst.Update("Exchange", "ftp://first/", null, null, 
            //0, 0, 
            0);
        new FileStorageRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("Exchange", "ftp://second/", null, null, 
            //0, 0, 
            0);
        new FileStorageRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        FileStorage? stored = await Stored("Exchange");
        Assert.NotNull(stored);
        Assert.Equal("ftp://first/", stored.FileStoragePath);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheFileStorageOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new FileStorageRepository(context).Delete(await Read(context, "Exchange"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["LocalBak"], await check.FileStorages.Select(x => x.Name).ToListAsync());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheFileStorageChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        FileStorage readFirst = await Read(first, "Exchange");
        FileStorage readSecond = await Read(second, "Exchange");
        readFirst.Update("Exchange", "ftp://first/", null, null, 
            //0, 0, 
            0);
        new FileStorageRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new FileStorageRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("Exchange"));
    }
}
