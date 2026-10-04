using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.ReactAppTemplates.DeleteReactAppTemplate;
using SupportToolsServer.Application.ReactAppTemplates.UpdateReactAppTemplate;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ReactAppTemplates;

//The handlers with the real repository and unit of work on SQLite, one context per request as in the host.
//The concurrent requests prove that the version check also holds between the read and the save of a handler
public sealed class ReactAppTemplateHandlersOnSqliteTests : IAsyncLifetime
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

    private async Task<Result<int>> Upsert(string name, string template, int version,
        Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateReactAppTemplateCommandHandler(new ReactAppTemplateRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(
            new UpdateReactAppTemplateCommand(TestData.ReactAppTemplateModel(name, template, version)),
            CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteReactAppTemplateCommandHandler(new ReactAppTemplateRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteReactAppTemplateCommand(name, version), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<ReactAppTemplate>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.ReactAppTemplates.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Upsert_CreatesTheRecordAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert("ReduxApp", "redux-typescript", 0)).Value);
        Assert.Equal(2, (await Upsert("ReduxApp", "cra-template-redux-typescript", 1)).Value);

        ReactAppTemplate stored = Assert.Single(await Stored());
        Assert.Equal("cra-template-redux-typescript", stored.Template);
        Assert.Equal(2, stored.Version);
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one record per name
    [Fact]
    public async Task Upsert_MatchesTheNameWithoutCase()
    {
        await Upsert("ReduxApp", "redux-typescript", 0);

        Result<int> created = await Upsert("REDUXAPP", "Other", 0);
        Result<int> updated = await Upsert("REDUXAPP", "redux-typescript", 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        ReactAppTemplate stored = Assert.Single(await Stored());
        Assert.Equal("REDUXAPP", stored.Name);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("ReduxApp", "redux-typescript", 0);

        Result<int> result = await Upsert("ReduxApp", "Mine", 1,
            async () => Assert.Equal(2, (await Upsert("ReduxApp", "Theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ReactAppTemplate ReduxApp Version Conflict: Expected 1, Actual 2", result.Error.Description);
        ReactAppTemplate stored = Assert.Single(await Stored());
        Assert.Equal("Theirs", stored.Template);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("ReduxApp", "Mine", 0,
            async () => Assert.Equal(1, (await Upsert("ReduxApp", "Theirs", 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ReactAppTemplate ReduxApp Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal("Theirs", Assert.Single(await Stored()).Template);
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert("ReduxApp", "redux-typescript", 0);

        Result<int> result = await Upsert("ReduxApp", "Mine", 1,
            async () => Assert.True((await Delete("ReduxApp", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordOfTheExpectedVersion_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("ReduxApp", "redux-typescript", 0);

        Result deleted = await Delete("REDUXAPP", 1);
        Result deletedAgain = await Delete("ReduxApp", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert("ReduxApp", "redux-typescript", 0);

        Result result = await Delete("ReduxApp", null,
            async () => Assert.Equal(2, (await Upsert("ReduxApp", "Theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ReactAppTemplate ReduxApp Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("Theirs", Assert.Single(await Stored()).Template);
    }
}
