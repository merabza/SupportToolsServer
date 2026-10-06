using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;
using SupportToolsServer.Application.SmartSchemas.UpdateSmartSchema;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.SmartSchemas;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.SmartSchemas;

//The handlers with the real repository and unit of work on SQLite, one context per request as in the host.
//The concurrent requests prove that the version check also holds between the read and the save of a handler, for
//the details as well
public sealed class SmartSchemaHandlersOnSqliteTests : IAsyncLifetime
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

    private async Task<Result<int>> Upsert(string name, int lastPreserveCount,
        IEnumerable<(string PeriodType, int PreserveCount)> details, int version,
        Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateSmartSchemaCommandHandler(new SmartSchemaRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(
            new UpdateSmartSchemaCommand(TestData.SmartSchemaModel(name, lastPreserveCount, details, version)),
            CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteSmartSchemaCommandHandler(new SmartSchemaRepository(context),
            new GlobalSettingsRepository(context), new ProjectCreatorSettingsRepository(context),
            new ProjectRepository(context), new ServerRepository(context),
            new DeploymentEnvironmentRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteSmartSchemaCommand(name, version), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<SmartSchema>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.SmartSchemas.AsNoTracking().Include(x => x.Details).ToListAsync();
    }

    //Every detail row of the database, so that orphans would show
    private async Task<List<string>> StoredDetailRows()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.Set<SmartSchemaDetail>().AsNoTracking().OrderBy(x => x.PeriodType)
            .Select(x => x.PeriodType + "=" + x.PreserveCount).ToListAsync();
    }

    [Fact]
    public async Task Upsert_CreatesTheRecordAndReplacesItsDetailsWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert("Reduce", 1, [("Day", 2), ("Month", 1), ("Year", 1)], 0)).Value);
        Assert.Equal(2, (await Upsert("Reduce", 3, [("Day", 7), ("Week", 4)], 1)).Value);

        SmartSchema stored = Assert.Single(await Stored());
        Assert.Equal(3, stored.LastPreserveCount);
        Assert.Equal(2, stored.Version);
        Assert.Equal(["Day=7", "Week=4"], await StoredDetailRows());
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one record per name
    [Fact]
    public async Task Upsert_MatchesTheNameWithoutCase()
    {
        await Upsert("Reduce", 1, [("Day", 2)], 0);

        Result<int> created = await Upsert("REDUCE", 1, [], 0);
        Result<int> updated = await Upsert("REDUCE", 1, [("Day", 3)], 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        SmartSchema stored = Assert.Single(await Stored());
        Assert.Equal("REDUCE", stored.Name);
        Assert.Equal(["Day=3"], await StoredDetailRows());
    }

    //Neither the schema nor the details of the request that lost the race are written
    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("Reduce", 1, [("Day", 2)], 0);

        Result<int> result = await Upsert("Reduce", 9, [("Hour", 9)], 1,
            async () => Assert.Equal(2, (await Upsert("Reduce", 2, [("Week", 3)], 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("SmartSchema Reduce Version Conflict: Expected 1, Actual 2", result.Error.Description);
        SmartSchema stored = Assert.Single(await Stored());
        Assert.Equal(2, stored.LastPreserveCount);
        Assert.Equal(2, stored.Version);
        Assert.Equal(["Week=3"], await StoredDetailRows());
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("Reduce", 9, [("Hour", 9)], 0,
            async () => Assert.Equal(1, (await Upsert("Reduce", 2, [("Week", 3)], 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("SmartSchema Reduce Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal(2, Assert.Single(await Stored()).LastPreserveCount);
        Assert.Equal(["Week=3"], await StoredDetailRows());
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert("Reduce", 1, [("Day", 2)], 0);

        Result<int> result = await Upsert("Reduce", 9, [("Hour", 9)], 1,
            async () => Assert.True((await Delete("Reduce", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
        Assert.Empty(await StoredDetailRows());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordWithItsDetails_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("Reduce", 1, [("Day", 2), ("Month", 1)], 0);
        await Upsert("Hourly", 1, [("Hour", 48)], 0);

        Result deleted = await Delete("reduce", 1);
        Result deletedAgain = await Delete("Reduce", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Equal("Hourly", Assert.Single(await Stored()).Name);
        Assert.Equal(["Hour=48"], await StoredDetailRows());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert("Reduce", 1, [("Day", 2)], 0);

        Result result = await Delete("Reduce", null,
            async () => Assert.Equal(2, (await Upsert("Reduce", 2, [("Week", 3)], 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("SmartSchema Reduce Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal(["Week=3"], await StoredDetailRows());
    }
}
