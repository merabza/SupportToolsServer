using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.StoredFiles;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class StoredFileRepositoryTests : IAsyncLifetime
{
    private const string FilePath = @"D:\1WorkSecurity\AppA\PAZISI\Prod\appsettings.json";

    private static readonly DateTime UpdatedAt = new(2026, 10, 7, 9, 30, 15, 123, DateTimeKind.Utc);

    private readonly StoredFile _appSettings = TestData.NewStoredFile(FilePath);
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.StoredFiles.AddRange(_appSettings, TestData.NewStoredFile(@"D:\1WorkSecurity\b.json", "abc", 4));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<StoredFile> Read(SupportToolsServerDbContext context, string path)
    {
        StoredFile? storedFile = await new StoredFileRepository(context).GetByPath(path, CancellationToken.None);
        return Assert.IsType<StoredFile>(storedFile);
    }

    private async Task<StoredFile?> Stored(string path)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.StoredFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Path == path);
    }

    [Fact]
    public async Task GetInfos_ReturnsTheMetadataOfEveryFileWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<StoredFileInfo> infos = await new StoredFileRepository(context).GetInfos(CancellationToken.None);

        Assert.Equal([
            new StoredFileInfo(FilePath, _appSettings.Sha256, _appSettings.Length, TestData.StoredFileTime, 1),
            new StoredFileInfo(@"D:\1WorkSecurity\b.json",
                "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", 3, TestData.StoredFileTime, 4)
        ], infos.OrderBy(x => x.Path, StringComparer.Ordinal));
        Assert.All(infos, x => Assert.Equal(DateTimeKind.Utc, x.UpdatedAtUtc.Kind));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //The list reads the metadata only: the content (nvarchar(max) on SQL Server) stays in the database
    [Fact]
    public async Task GetInfos_DoesNotReadTheContent()
    {
        var commands = new ReaderCommandCollector();
        await using SupportToolsServerDbContext context = _database.NewContextWithInterceptor(commands);

        await new StoredFileRepository(context).GetInfos(CancellationToken.None);

        string select = Assert.Single(commands.CommandTexts);
        Assert.Contains("\"Sha256\"", select, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Content\"", select, StringComparison.Ordinal);
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData(FilePath)]
    [InlineData(@"d:\1worksecurity\appa\pazisi\prod\appsettings.json")]
    [InlineData(@"D:\1WORKSECURITY\APPA\PAZISI\PROD\APPSETTINGS.JSON")]
    public async Task GetByPath_FindsThePathWithoutCaseAndWithoutTrackingIt(string path)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        StoredFile found = await Read(context, path);

        Assert.Equal(_appSettings.Id, found.Id);
        Assert.Equal(FilePath, found.Path);
        Assert.Equal(TestData.MadeUpFileContent, found.Content);
        Assert.Equal(_appSettings.Sha256, found.Sha256);
        Assert.Equal(_appSettings.Length, found.Length);
        Assert.Equal(TestData.StoredFileTime, found.UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, found.UpdatedAtUtc.Kind);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //Only the ids and the paths are read to find the file, and its content only when it is found
    [Fact]
    public async Task GetByPath_ReturnsNullWithoutReadingAnyContent_WhenThereIsNoSuchPath()
    {
        var commands = new ReaderCommandCollector();
        await using SupportToolsServerDbContext context = _database.NewContextWithInterceptor(commands);

        StoredFile? found = await new StoredFileRepository(context).GetByPath(@"D:\x.json", CancellationToken.None);

        Assert.Null(found);
        Assert.DoesNotContain("\"Content\"", Assert.Single(commands.CommandTexts), StringComparison.Ordinal);
    }

    //The line breaks, the tab, the quotes and the Georgian letters of the content are stored unchanged
    [Fact]
    public async Task Add_StoresTheFileWithItsFirstVersionOnSave()
    {
        const string content = "{\r\n\t\"Text\": \"ქართული \\\"made-up\\\"\"\n}";
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new StoredFileRepository(context).Add(StoredFile.Create(@"D:\1WorkSecurity\c.json", content, UpdatedAt));
            await context.SaveChangesAsync();
        }

        StoredFile? stored = await Stored(@"D:\1WorkSecurity\c.json");
        Assert.NotNull(stored);
        Assert.Equal(content, stored.Content);
        Assert.Equal(StoredFile.Create("x", content, UpdatedAt).Sha256, stored.Sha256);
        Assert.Equal(49, stored.Length);
        Assert.Equal(UpdatedAt, stored.UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, stored.UpdatedAtUtc.Kind);
        Assert.Equal(1, stored.Version);
    }

    //The content has no length in the database, so the largest content that the validator accepts is not cut
    [Fact]
    public async Task Add_StoresAContentOfTheMaximumSizeUnchanged()
    {
        string content = new('a', 1048576);
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new StoredFileRepository(context).Add(StoredFile.Create(@"D:\large.json", content, UpdatedAt));
            await context.SaveChangesAsync();
        }

        StoredFile? stored = await Stored(@"D:\large.json");
        Assert.NotNull(stored);
        Assert.Equal(content, stored.Content);
        Assert.Equal(1048576, stored.Length);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenThePathIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new StoredFileRepository(context).Add(StoredFile.Create(FilePath, "other", UpdatedAt));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadFileWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            StoredFile read = await Read(context, FilePath);
            read.Update(FilePath.ToUpperInvariant(), "abc", UpdatedAt);
            new StoredFileRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        StoredFile? stored = await Stored(FilePath.ToUpperInvariant());
        Assert.NotNull(stored);
        Assert.Equal(_appSettings.Id, stored.Id);
        Assert.Equal("abc", stored.Content);
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", stored.Sha256);
        Assert.Equal(3, stored.Length);
        Assert.Equal(UpdatedAt, stored.UpdatedAtUtc);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheFileChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        StoredFile readFirst = await Read(first, FilePath);
        StoredFile readSecond = await Read(second, FilePath);
        readFirst.Update(FilePath, "first", UpdatedAt);
        new StoredFileRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update(FilePath, "second", UpdatedAt);
        new StoredFileRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        StoredFile? stored = await Stored(FilePath);
        Assert.NotNull(stored);
        Assert.Equal("first", stored.Content);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheFileOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new StoredFileRepository(context).Delete(await Read(context, FilePath));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal([@"D:\1WorkSecurity\b.json"], await check.StoredFiles.Select(x => x.Path).ToListAsync());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheFileChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        StoredFile readFirst = await Read(first, FilePath);
        StoredFile readSecond = await Read(second, FilePath);
        readFirst.Update(FilePath, "first", UpdatedAt);
        new StoredFileRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new StoredFileRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored(FilePath));
    }
}
