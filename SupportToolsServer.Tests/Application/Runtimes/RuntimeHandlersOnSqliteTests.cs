using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.Runtimes.DeleteRuntime;
using SupportToolsServer.Application.Runtimes.UpdateRuntime;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Runtimes;

//The handlers with the real repository and unit of work on SQLite, one context per request as in the host.
//The concurrent requests prove that the version check also holds between the read and the save of a handler
public sealed class RuntimeHandlersOnSqliteTests : IAsyncLifetime
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

    private async Task<Result<int>> Upsert(string name, string? description, int version,
        Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateRuntimeCommandHandler(new RuntimeRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new UpdateRuntimeCommand(TestData.RuntimeModel(name, description, version)),
            CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteRuntimeCommandHandler(new RuntimeRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteRuntimeCommand(name, version), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<Runtime>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.Runtimes.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Upsert_CreatesTheRecordAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert("win-x64", "Windows x64", 0)).Value);
        Assert.Equal(2, (await Upsert("win-x64", "Windows 64 bit", 1)).Value);

        Runtime stored = Assert.Single(await Stored());
        Assert.Equal("Windows 64 bit", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one record per name
    [Fact]
    public async Task Upsert_MatchesTheNameWithoutCase()
    {
        await Upsert("win-x64", "Windows x64", 0);

        Result<int> created = await Upsert("WIN-X64", "Other", 0);
        Result<int> updated = await Upsert("WIN-X64", "Windows x64", 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        Runtime stored = Assert.Single(await Stored());
        Assert.Equal("WIN-X64", stored.Name);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("win-x64", "Windows x64", 0);

        Result<int> result = await Upsert("win-x64", "Mine", 1,
            async () => Assert.Equal(2, (await Upsert("win-x64", "Theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Runtime win-x64 Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Runtime stored = Assert.Single(await Stored());
        Assert.Equal("Theirs", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("win-x64", "Mine", 0,
            async () => Assert.Equal(1, (await Upsert("win-x64", "Theirs", 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Runtime win-x64 Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal("Theirs", Assert.Single(await Stored()).Description);
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert("win-x64", "Windows x64", 0);

        Result<int> result = await Upsert("win-x64", "Mine", 1,
            async () => Assert.True((await Delete("win-x64", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordOfTheExpectedVersion_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("win-x64", "Windows x64", 0);

        Result deleted = await Delete("WIN-X64", 1);
        Result deletedAgain = await Delete("win-x64", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert("win-x64", "Windows x64", 0);

        Result result = await Delete("win-x64", null,
            async () => Assert.Equal(2, (await Upsert("win-x64", "Theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Runtime win-x64 Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("Theirs", Assert.Single(await Stored()).Description);
    }
}
