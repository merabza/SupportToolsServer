using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.StoredFiles.DeleteStoredFile;
using SupportToolsServer.Application.StoredFiles.GetStoredFileByPath;
using SupportToolsServer.Application.StoredFiles.GetStoredFiles;
using SupportToolsServer.Application.StoredFiles.UpdateStoredFile;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.StoredFiles;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.StoredFiles;

//The handlers with the real repository and unit of work on SQLite, one context per request as in the host.
//The concurrent requests prove that the version check also holds between the read and the save of a handler
public sealed class StoredFileHandlersOnSqliteTests : IAsyncLifetime
{
    private const string FilePath = @"D:\1WorkSecurity\AppA\PAZISI\Prod\appsettings.json";
    private const string LowerCaseFilePath = @"d:\1worksecurity\appa\pazisi\prod\appsettings.json";

    private static readonly DateTimeOffset Now = new(2026, 10, 7, 9, 30, 15, TimeSpan.Zero);

    private readonly Mock<TimeProvider> _timeProvider = new();
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(Now);
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private async Task<Result<int>> Upsert(string path, string content, int version,
        Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateStoredFileCommandHandler(new StoredFileRepository(context), _timeProvider.Object,
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new UpdateStoredFileCommand(TestData.StoredFileModel(path, content, version)),
            CancellationToken.None);
    }

    private async Task<Result> Delete(string path, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteStoredFileCommandHandler(new StoredFileRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteStoredFileCommand(path, version), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<StoredFile>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.StoredFiles.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Upsert_CreatesTheFileAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert(FilePath, "abc", 0)).Value);
        Assert.Equal(2, (await Upsert(FilePath, TestData.MadeUpFileContent, 1)).Value);

        StoredFile stored = Assert.Single(await Stored());
        Assert.Equal(TestData.MadeUpFileContent, stored.Content);
        Assert.Equal(TestData.NewStoredFile(FilePath).Sha256, stored.Sha256);
        Assert.Equal(TestData.MadeUpFileContent.Length, stored.Length);
        Assert.Equal(Now.UtcDateTime, stored.UpdatedAtUtc);
        Assert.Equal(2, stored.Version);
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one file per path
    [Fact]
    public async Task Upsert_MatchesThePathWithoutCase()
    {
        await Upsert(FilePath, "abc", 0);

        Result<int> created = await Upsert(FilePath.ToUpperInvariant(), "other", 0);
        Result<int> updated = await Upsert(FilePath.ToUpperInvariant(), "abc", 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        StoredFile stored = Assert.Single(await Stored());
        Assert.Equal(FilePath.ToUpperInvariant(), stored.Path);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert(FilePath, "abc", 0);

        Result<int> result = await Upsert(FilePath, "mine", 1,
            async () => Assert.Equal(2, (await Upsert(FilePath, "theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected 1, Actual 2", result.Error.Description);
        StoredFile stored = Assert.Single(await Stored());
        Assert.Equal("theirs", stored.Content);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesThePathBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert(FilePath, "mine", 0,
            async () => Assert.Equal(1, (await Upsert(FilePath, "theirs", 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal("theirs", Assert.Single(await Stored()).Content);
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert(FilePath, "abc", 0);

        Result<int> result =
            await Upsert(FilePath, "mine", 1, async () => Assert.True((await Delete(FilePath, 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheFileOfTheExpectedVersion_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert(FilePath, "abc", 0);

        Result deleted = await Delete(LowerCaseFilePath, 1);
        Result deletedAgain = await Delete(FilePath, 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert(FilePath, "abc", 0);

        Result result = await Delete(FilePath, null,
            async () => Assert.Equal(2, (await Upsert(FilePath, "theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("theirs", Assert.Single(await Stored()).Content);
    }

    //The list and the content of the files that the upserts stored
    [Fact]
    public async Task Queries_ReturnTheMetadataOfTheStoredFilesAndTheContentOfOne()
    {
        await Upsert(@"D:\1WorkSecurity\b.json", "abc", 0);
        await Upsert(FilePath, TestData.MadeUpFileContent, 0);
        await using SupportToolsServerDbContext context = _database.NewContext();
        var repository = new StoredFileRepository(context);

        Result<List<StsStoredFileInfoDataModel>> list =
            await new GetStoredFilesQueryHandler(repository).Handle(new GetStoredFilesQuery(), CancellationToken.None);
        Result<StsStoredFileDataModel> file = await new GetStoredFileByPathQueryHandler(repository).Handle(
            new GetStoredFileByPathQuery(LowerCaseFilePath), CancellationToken.None);

        Assert.Equal([FilePath, @"D:\1WorkSecurity\b.json"], list.Value.Select(x => x.Path));
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", list.Value[1].Sha256);
        Assert.Equal(3, list.Value[1].Length);
        Assert.Equal(Now.UtcDateTime, list.Value[1].UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, list.Value[1].UpdatedAtUtc.Kind);
        Assert.All(list.Value, x => Assert.Equal(1, x.Version));
        Assert.Equal(FilePath, file.Value.Path);
        Assert.Equal(TestData.MadeUpFileContent, file.Value.Content);
        Assert.Equal(1, file.Value.Version);
    }
}
