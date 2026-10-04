using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.DotnetTools.DeleteDotnetTool;
using SupportToolsServer.Application.DotnetTools.UpdateDotnetTool;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DotnetTools;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.DotnetTools;

//The handlers with the real repository and unit of work on SQLite, one context per request as in the host.
//The concurrent requests prove that the version check also holds between the read and the save of a handler
public sealed class DotnetToolHandlersOnSqliteTests : IAsyncLifetime
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

    private Task<Result<int>> Upsert(string name, string? description, int version, Func<Task>? concurrentChange = null)
    {
        return Upsert(TestData.DotnetToolModel(name, "dotnet-ef", null, description, version), concurrentChange);
    }

    private async Task<Result<int>> Upsert(StsDotnetToolDataModel model, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateDotnetToolCommandHandler(new DotnetToolRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new UpdateDotnetToolCommand(model), CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteDotnetToolCommandHandler(new DotnetToolRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteDotnetToolCommand(name, version), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<DotnetTool>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.DotnetTools.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Upsert_CreatesTheRecordAndUpdatesEveryValueWithTheVersionsItReturns()
    {
        Assert.Equal(1,
            (await Upsert(TestData.DotnetToolModel("DotnetEf", "dotnet-ef", "9.0.8", "Entity Framework"))).Value);
        Assert.Equal(2,
            (await Upsert(TestData.DotnetToolModel("DotnetEf", "Dotnet-Ef", null, "EF Core tools", 1))).Value);

        DotnetTool stored = Assert.Single(await Stored());
        Assert.Equal("Dotnet-Ef", stored.PackageId);
        Assert.Null(stored.MaxVersion);
        Assert.Equal("EF Core tools", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one record per name
    [Fact]
    public async Task Upsert_MatchesTheNameWithoutCase()
    {
        await Upsert("DotnetEf", "Entity Framework", 0);

        Result<int> created = await Upsert("DOTNETEF", "Other", 0);
        Result<int> updated = await Upsert("DOTNETEF", "Entity Framework", 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        DotnetTool stored = Assert.Single(await Stored());
        Assert.Equal("DOTNETEF", stored.Name);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("DotnetEf", "Entity Framework", 0);

        Result<int> result = await Upsert("DotnetEf", "Mine", 1,
            async () => Assert.Equal(2, (await Upsert("DotnetEf", "Theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DotnetTool DotnetEf Version Conflict: Expected 1, Actual 2", result.Error.Description);
        DotnetTool stored = Assert.Single(await Stored());
        Assert.Equal("Theirs", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("DotnetEf", "Mine", 0,
            async () => Assert.Equal(1, (await Upsert("DotnetEf", "Theirs", 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DotnetTool DotnetEf Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal("Theirs", Assert.Single(await Stored()).Description);
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert("DotnetEf", "Entity Framework", 0);

        Result<int> result = await Upsert("DotnetEf", "Mine", 1,
            async () => Assert.True((await Delete("DotnetEf", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordOfTheExpectedVersion_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("DotnetEf", "Entity Framework", 0);

        Result deleted = await Delete("DOTNETEF", 1);
        Result deletedAgain = await Delete("DotnetEf", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert("DotnetEf", "Entity Framework", 0);

        Result result = await Delete("DotnetEf", null,
            async () => Assert.Equal(2, (await Upsert("DotnetEf", "Theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DotnetTool DotnetEf Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("Theirs", Assert.Single(await Stored()).Description);
    }
}
