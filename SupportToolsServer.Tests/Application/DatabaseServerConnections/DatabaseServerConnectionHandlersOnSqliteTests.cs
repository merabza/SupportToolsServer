using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.ApiClients.DeleteApiClient;
using SupportToolsServer.Application.ApiClients.UpdateApiClient;
using SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;
using SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnectionByName;
using SupportToolsServer.Application.DatabaseServerConnections.UpdateDatabaseServerConnection;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.DatabaseServerConnections;

//The handlers with the real repositories and unit of work on SQLite, one context per request as in the host.
//Besides the versions of the connection and its folders sets, they show the web agent reference of both sides: a
//connection cannot name a missing ApiClient, and an ApiClient that a connection uses cannot be deleted
public sealed class DatabaseServerConnectionHandlersOnSqliteTests : IAsyncLifetime
{
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        Assert.Equal(1, (await UpsertApiClient("Pc1.WebAgent", 0)).Value);
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private async Task<Result<int>> Upsert(string name, string? dbWebAgentName, IEnumerable<string> foldersSetNames,
        int version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateDatabaseServerConnectionCommandHandler(new DatabaseServerConnectionRepository(context),
            new ApiClientRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(
            new UpdateDatabaseServerConnectionCommand(TestData.DatabaseServerConnectionModel(name, dbWebAgentName,
                foldersSetNames, version)), CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteDatabaseServerConnectionCommandHandler(new DatabaseServerConnectionRepository(context),
            new ProjectCreatorSettingsRepository(context), new ProjectRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteDatabaseServerConnectionCommand(name, version), CancellationToken.None);
    }

    private async Task<Result<int>> UpsertApiClient(string name, int version)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateApiClientCommandHandler(new ApiClientRepository(context),
            new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new UpdateApiClientCommand(TestData.ApiClientModel(name, version: version)),
            CancellationToken.None);
    }

    private async Task<Result> DeleteApiClient(string name, int? version)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteApiClientCommandHandler(new ApiClientRepository(context),
            new DatabaseServerConnectionRepository(context), new ServerRepository(context),
            new GlobalSettingsRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteApiClientCommand(name, version), CancellationToken.None);
    }

    private async Task<Result<StsDatabaseServerConnectionDataModel>> Get(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new GetDatabaseServerConnectionByNameQueryHandler(new DatabaseServerConnectionRepository(context),
            new ApiClientRepository(context));
        return await handler.Handle(new GetDatabaseServerConnectionByNameQuery(name), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<DatabaseServerConnection>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.DatabaseServerConnections.AsNoTracking().Include(x => x.DatabaseFoldersSets)
            .ToListAsync();
    }

    //Every folders set row of the database, so that orphans would show
    private async Task<List<string>> StoredFoldersSetRows()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.Set<DatabaseFoldersSet>().AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name)
            .ToListAsync();
    }

    [Fact]
    public async Task Upsert_CreatesTheRecordAndReplacesItsFoldersSetsWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert("Pc1.Sql", "Pc1.WebAgent", ["Default", "Old"], 0)).Value);
        Assert.Equal(2, (await Upsert("Pc1.Sql", "PC1.WEBAGENT", ["Default", "New"], 1)).Value);

        DatabaseServerConnection stored = Assert.Single(await Stored());
        Assert.Equal(2, stored.Version);
        Assert.Equal(["Default", "New"], await StoredFoldersSetRows());
        StsDatabaseServerConnectionDataModel read = (await Get("pc1.sql")).Value;
        Assert.Equal("Pc1.WebAgent", read.DbWebAgentName);
        Assert.Equal(["Default", "New"], read.DatabaseFoldersSets.Select(x => x.Name));
    }

    //The reference is checked before anything is written
    [Fact]
    public async Task Upsert_ReturnsReferencedRecordsNotFound_WhenTheWebAgentDoesNotExist()
    {
        Result<int> result = await Upsert("Pc1.Sql", "Pc2.WebAgent", ["Default"], 0);

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal("Referenced ApiClient Records Not Found: Pc2.WebAgent", result.Error.Description);
        Assert.Empty(await Stored());
        Assert.Empty(await StoredFoldersSetRows());
    }

    [Fact]
    public async Task Upsert_RemovesTheWebAgent_WhenTheBodyNamesNone()
    {
        await Upsert("Pc1.Sql", "Pc1.WebAgent", [], 0);

        Assert.Equal(2, (await Upsert("Pc1.Sql", null, [], 1)).Value);

        Assert.Null(Assert.Single(await Stored()).DbWebAgentId);
        Assert.Null((await Get("Pc1.Sql")).Value.DbWebAgentName);
    }

    //Neither the connection nor the folders sets of the request that lost the race are written
    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("Pc1.Sql", null, ["Default"], 0);

        Result<int> result = await Upsert("Pc1.Sql", null, ["Mine"], 1,
            async () => Assert.Equal(2, (await Upsert("Pc1.Sql", "Pc1.WebAgent", ["Theirs"], 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DatabaseServerConnection Pc1.Sql Version Conflict: Expected 1, Actual 2",
            result.Error.Description);
        Assert.Equal(2, Assert.Single(await Stored()).Version);
        Assert.Equal(["Theirs"], await StoredFoldersSetRows());
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("Pc1.Sql", null, ["Mine"], 0,
            async () => Assert.Equal(1, (await Upsert("Pc1.Sql", null, ["Theirs"], 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DatabaseServerConnection Pc1.Sql Version Conflict: Expected 0, Actual 1",
            result.Error.Description);
        Assert.Equal(["Theirs"], await StoredFoldersSetRows());
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert("Pc1.Sql", null, ["Default"], 0);

        Result<int> result = await Upsert("Pc1.Sql", null, ["Mine"], 1,
            async () => Assert.True((await Delete("Pc1.Sql", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
        Assert.Empty(await StoredFoldersSetRows());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordWithItsFoldersSets_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("Pc1.Sql", "Pc1.WebAgent", ["Default", "Second"], 0);

        Result deleted = await Delete("PC1.SQL", 1);
        Result deletedAgain = await Delete("Pc1.Sql", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Empty(await StoredFoldersSetRows());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    //The ApiClient stays while a connection uses it, and can be deleted once the connection no longer does
    [Fact]
    public async Task DeleteApiClient_ReturnsRecordIsInUse_UntilNoConnectionUsesIt()
    {
        await Upsert("Pc1.Sql", "Pc1.WebAgent", [], 0);

        Result refused = await DeleteApiClient("pc1.webagent", 1);
        await Upsert("Pc1.Sql", null, [], 1);
        Result deleted = await DeleteApiClient("Pc1.WebAgent", 1);

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal("ApiClient pc1.webagent Is Used By: DatabaseServerConnection Pc1.Sql",
            refused.Error.Description);
        Assert.True(deleted.IsSuccess);
        Assert.Equal("RecordWithNameNotFound", (await DeleteApiClient("Pc1.WebAgent", null)).Error.Code);
    }
}
