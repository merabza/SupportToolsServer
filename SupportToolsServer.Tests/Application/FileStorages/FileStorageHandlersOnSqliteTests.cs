using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.FileStorages.DeleteFileStorage;
using SupportToolsServer.Application.FileStorages.UpdateFileStorage;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.FileStorages;

//The handlers with the real repository and unit of work on SQLite, one context per request as in the host.
//The concurrent requests prove that the version check also holds between the read and the save of a handler
public sealed class FileStorageHandlersOnSqliteTests : IAsyncLifetime
{
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private async Task<Result<int>> Upsert(string name, string? fileStoragePath, int version,
        Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateFileStorageCommandHandler(new FileStorageRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(
            new UpdateFileStorageCommand(TestData.FileStorageModel(name, fileStoragePath, version: version)),
            CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteFileStorageCommandHandler(new FileStorageRepository(context),
            new GlobalSettingsRepository(context), new ProjectCreatorSettingsRepository(context),
            new ProjectRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteFileStorageCommand(name, version), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<FileStorage>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.FileStorages.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Upsert_CreatesTheRecordAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert("Exchange", "ftp://a/", 0)).Value);
        Assert.Equal(2, (await Upsert("Exchange", "ftp://b/", 1)).Value);

        FileStorage stored = Assert.Single(await Stored());
        Assert.Equal("ftp://b/", stored.FileStoragePath);
        Assert.Equal(TestData.MadeUpPassword, stored.Password);
        Assert.Equal(2, stored.Version);
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one record per name
    [Fact]
    public async Task Upsert_MatchesTheNameWithoutCase()
    {
        await Upsert("Exchange", "ftp://a/", 0);

        Result<int> created = await Upsert("EXCHANGE", "ftp://other/", 0);
        Result<int> updated = await Upsert("EXCHANGE", "ftp://a/", 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        Assert.Equal("EXCHANGE", Assert.Single(await Stored()).Name);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("Exchange", "ftp://a/", 0);

        Result<int> result = await Upsert("Exchange", "ftp://mine/", 1,
            async () => Assert.Equal(2, (await Upsert("Exchange", "ftp://theirs/", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("FileStorage Exchange Version Conflict: Expected 1, Actual 2", result.Error.Description);
        FileStorage stored = Assert.Single(await Stored());
        Assert.Equal("ftp://theirs/", stored.FileStoragePath);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("Exchange", "ftp://mine/", 0,
            async () => Assert.Equal(1, (await Upsert("Exchange", "ftp://theirs/", 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("FileStorage Exchange Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal("ftp://theirs/", Assert.Single(await Stored()).FileStoragePath);
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert("Exchange", "ftp://a/", 0);

        Result<int> result = await Upsert("Exchange", "ftp://mine/", 1,
            async () => Assert.True((await Delete("Exchange", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordOfTheExpectedVersion_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("Exchange", "ftp://a/", 0);

        Result deleted = await Delete("exchange", 1);
        Result deletedAgain = await Delete("Exchange", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert("Exchange", "ftp://a/", 0);

        Result result = await Delete("Exchange", null,
            async () => Assert.Equal(2, (await Upsert("Exchange", "ftp://theirs/", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("FileStorage Exchange Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("ftp://theirs/", Assert.Single(await Stored()).FileStoragePath);
    }
}
