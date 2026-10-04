using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.ApiClients.DeleteApiClient;
using SupportToolsServer.Application.ApiClients.UpdateApiClient;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ApiClients;

//The handlers with the real repositories and unit of work on SQLite, one context per request as in the host.
//The concurrent requests prove that the version check also holds between the read and the save of a handler.
//The connections that use an ApiClient are in DatabaseServerConnectionHandlersOnSqliteTests
public sealed class ApiClientHandlersOnSqliteTests : IAsyncLifetime
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

    private async Task<Result<int>> Upsert(string name, string? server, int version,
        Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateApiClientCommandHandler(new ApiClientRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new UpdateApiClientCommand(TestData.ApiClientModel(name, server, version: version)),
            CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteApiClientCommandHandler(new ApiClientRepository(context),
            new DatabaseServerConnectionRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteApiClientCommand(name, version), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<ApiClient>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.ApiClients.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Upsert_CreatesTheRecordAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert("Pc1.WebAgent", "http://a/", 0)).Value);
        Assert.Equal(2, (await Upsert("Pc1.WebAgent", "http://b/", 1)).Value);

        ApiClient stored = Assert.Single(await Stored());
        Assert.Equal("http://b/", stored.Server);
        Assert.Equal(TestData.MadeUpApiKey, stored.ApiKey);
        Assert.Equal(2, stored.Version);
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one record per name
    [Fact]
    public async Task Upsert_MatchesTheNameWithoutCase()
    {
        await Upsert("Pc1.WebAgent", "http://a/", 0);

        Result<int> created = await Upsert("PC1.WEBAGENT", "http://other/", 0);
        Result<int> updated = await Upsert("PC1.WEBAGENT", "http://a/", 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        Assert.Equal("PC1.WEBAGENT", Assert.Single(await Stored()).Name);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("Pc1.WebAgent", "http://a/", 0);

        Result<int> result = await Upsert("Pc1.WebAgent", "http://mine/", 1,
            async () => Assert.Equal(2, (await Upsert("Pc1.WebAgent", "http://theirs/", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ApiClient Pc1.WebAgent Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("http://theirs/", Assert.Single(await Stored()).Server);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("Pc1.WebAgent", "http://mine/", 0,
            async () => Assert.Equal(1, (await Upsert("Pc1.WebAgent", "http://theirs/", 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ApiClient Pc1.WebAgent Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal("http://theirs/", Assert.Single(await Stored()).Server);
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert("Pc1.WebAgent", "http://a/", 0);

        Result<int> result = await Upsert("Pc1.WebAgent", "http://mine/", 1,
            async () => Assert.True((await Delete("Pc1.WebAgent", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordOfTheExpectedVersion_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("Pc1.WebAgent", "http://a/", 0);

        Result deleted = await Delete("pc1.webagent", 1);
        Result deletedAgain = await Delete("Pc1.WebAgent", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert("Pc1.WebAgent", "http://a/", 0);

        Result result = await Delete("Pc1.WebAgent", null,
            async () => Assert.Equal(2, (await Upsert("Pc1.WebAgent", "http://theirs/", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ApiClient Pc1.WebAgent Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("http://theirs/", Assert.Single(await Stored()).Server);
    }
}
