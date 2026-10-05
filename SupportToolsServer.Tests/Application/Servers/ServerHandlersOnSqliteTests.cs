using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.ApiClients.DeleteApiClient;
using SupportToolsServer.Application.ApiClients.UpdateApiClient;
using SupportToolsServer.Application.Runtimes.DeleteRuntime;
using SupportToolsServer.Application.Runtimes.UpdateRuntime;
using SupportToolsServer.Application.Servers.DeleteServer;
using SupportToolsServer.Application.Servers.GetServerByName;
using SupportToolsServer.Application.Servers.UpdateServer;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Servers;

//The handlers with the real repositories and unit of work on SQLite, one context per request as in the host.
//Besides the versions, they show the references of both sides: a server cannot name a missing ApiClient or Runtime,
//and an ApiClient or a Runtime that a server uses cannot be deleted
public sealed class ServerHandlersOnSqliteTests : IAsyncLifetime
{
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        Assert.Equal(1, (await UpsertApiClient("Dl360.WebAgent")).Value);
        Assert.Equal(1, (await UpsertApiClient("Dl360.Installer")).Value);
        Assert.Equal(1, (await UpsertRuntime("linux-x64")).Value);
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private async Task<Result<int>> Upsert(string name, string? webAgentName, string? webAgentInstallerName,
        string? runtime, int version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateServerCommandHandler(new ServerRepository(context), new ApiClientRepository(context),
            new RuntimeRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(
            new UpdateServerCommand(TestData.ServerModel(name, webAgentName, webAgentInstallerName, runtime,
                version)), CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteServerCommandHandler(new ServerRepository(context),
            new ProjectCreatorSettingsRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteServerCommand(name, version), CancellationToken.None);
    }

    private async Task<Result<StsServerDataModel>> Get(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new GetServerByNameQueryHandler(new ServerRepository(context), new ApiClientRepository(context),
            new RuntimeRepository(context));
        return await handler.Handle(new GetServerByNameQuery(name), CancellationToken.None);
    }

    private async Task<Result<int>> UpsertApiClient(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateApiClientCommandHandler(new ApiClientRepository(context),
            new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new UpdateApiClientCommand(TestData.ApiClientModel(name)), CancellationToken.None);
    }

    private async Task<Result> DeleteApiClient(string name, int? version)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteApiClientCommandHandler(new ApiClientRepository(context),
            new DatabaseServerConnectionRepository(context), new ServerRepository(context),
            new GlobalSettingsRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteApiClientCommand(name, version), CancellationToken.None);
    }

    private async Task<Result<int>> UpsertRuntime(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateRuntimeCommandHandler(new RuntimeRepository(context),
            new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new UpdateRuntimeCommand(TestData.RuntimeModel(name)), CancellationToken.None);
    }

    private async Task<Result> DeleteRuntime(string name, int? version)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteRuntimeCommandHandler(new RuntimeRepository(context), new ServerRepository(context),
            new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteRuntimeCommand(name, version), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<Server>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.Servers.AsNoTracking().ToListAsync();
    }

    //The references match without case and are read back with the stored names
    [Fact]
    public async Task Upsert_CreatesTheRecordAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert("dl360", "Dl360.WebAgent", "Dl360.Installer", "linux-x64", 0)).Value);
        Assert.Equal(2, (await Upsert("dl360", "DL360.INSTALLER", null, "LINUX-X64", 1)).Value);

        Assert.Equal(2, Assert.Single(await Stored()).Version);
        StsServerDataModel read = (await Get("DL360")).Value;
        Assert.Equal("dl360", read.Name);
        Assert.Equal("Dl360.Installer", read.WebAgentName);
        Assert.Null(read.WebAgentInstallerName);
        Assert.Equal("linux-x64", read.Runtime);
        Assert.Equal(2, read.Version);
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one record per name
    [Fact]
    public async Task Upsert_MatchesTheNameWithoutCase()
    {
        await Upsert("dl360", null, null, null, 0);

        Result<int> created = await Upsert("DL360", null, null, null, 0);
        Result<int> updated = await Upsert("DL360", null, null, null, 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        Assert.Equal("DL360", Assert.Single(await Stored()).Name);
    }

    //The references are checked before anything is written
    [Fact]
    public async Task Upsert_ReturnsReferencedRecordsNotFoundWithEveryMissingName()
    {
        Result<int> result = await Upsert("dl360", "Pc9.WebAgent", "Dl360.Installer", "osx-arm64", 0);

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal("Referenced ApiClient Records Not Found: Pc9.WebAgent; " +
                     "Referenced Runtime Records Not Found: osx-arm64", result.Error.Description);
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Upsert_RemovesTheReferences_WhenTheBodyNamesNone()
    {
        await Upsert("dl360", "Dl360.WebAgent", "Dl360.Installer", "linux-x64", 0);

        Assert.Equal(2, (await Upsert("dl360", null, "", " ", 1)).Value);

        Server stored = Assert.Single(await Stored());
        Assert.Null(stored.WebAgentId);
        Assert.Null(stored.WebAgentInstallerId);
        Assert.Null(stored.RuntimeId);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("dl360", null, null, null, 0);

        Result<int> result = await Upsert("dl360", "Dl360.WebAgent", null, null, 1,
            async () => Assert.Equal(2, (await Upsert("dl360", null, null, "linux-x64", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Server dl360 Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Server stored = Assert.Single(await Stored());
        Assert.Null(stored.WebAgentId);
        Assert.NotNull(stored.RuntimeId);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("dl360", "Dl360.WebAgent", null, null, 0,
            async () => Assert.Equal(1, (await Upsert("dl360", null, null, "linux-x64", 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Server dl360 Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Null(Assert.Single(await Stored()).WebAgentId);
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert("dl360", null, null, null, 0);

        Result<int> result = await Upsert("dl360", null, null, "linux-x64", 1,
            async () => Assert.True((await Delete("dl360", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
    }

    //The foreign key keeps the reference that was deleted after the check; the version still matches, so the save
    //fails with the exception of the database (CLAUDE.md, Registry conventions: a 500)
    [Fact]
    public async Task Upsert_Throws_WhenAnotherRequestDeletesTheRuntimeBetweenTheCheckAndTheSave()
    {
        await Assert.ThrowsAsync<DbUpdateException>(() => Upsert("dl360", null, null, "linux-x64", 0,
            async () => Assert.True((await DeleteRuntime("linux-x64", 1)).IsSuccess)));

        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordOfTheExpectedVersion_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("dl360", "Dl360.WebAgent", "Dl360.Installer", "linux-x64", 0);

        Result deleted = await Delete("DL360", 1);
        Result deletedAgain = await Delete("dl360", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert("dl360", null, null, null, 0);

        Result result = await Delete("dl360", null,
            async () => Assert.Equal(2, (await Upsert("dl360", null, null, "linux-x64", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Server dl360 Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Single(await Stored());
    }

    //The Runtime stays while a server uses it, and can be deleted once no server does
    [Fact]
    public async Task DeleteRuntime_ReturnsRecordIsInUse_UntilNoServerUsesIt()
    {
        await Upsert("dl360", null, null, "linux-x64", 0);
        await Upsert("bee", null, null, "LINUX-X64", 0);

        Result refused = await DeleteRuntime("Linux-X64", 1);
        await Upsert("dl360", null, null, null, 1);
        await Delete("bee", 1);
        Result deleted = await DeleteRuntime("linux-x64", 1);

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal("Runtime Linux-X64 Is Used By: Server bee, Server dl360", refused.Error.Description);
        Assert.True(deleted.IsSuccess);
    }

    //Both web agent fields count; a server that uses the ApiClient in both is named once
    [Fact]
    public async Task DeleteApiClient_ReturnsRecordIsInUse_UntilNoServerUsesIt()
    {
        await Upsert("dl360", "Dl360.WebAgent", "Dl360.WebAgent", null, 0);
        await Upsert("bee", null, "Dl360.WebAgent", null, 0);

        Result refused = await DeleteApiClient("dl360.webagent", 1);
        await Upsert("dl360", null, "Dl360.Installer", null, 1);
        Result stillRefused = await DeleteApiClient("Dl360.WebAgent", 1);
        await Upsert("bee", null, null, null, 1);
        Result deleted = await DeleteApiClient("Dl360.WebAgent", 1);

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal("ApiClient dl360.webagent Is Used By: Server bee, Server dl360", refused.Error.Description);
        Assert.Equal("ApiClient Dl360.WebAgent Is Used By: Server bee", stillRefused.Error.Description);
        Assert.True(deleted.IsSuccess);
        Assert.Equal("RecordIsInUse", (await DeleteApiClient("Dl360.Installer", 1)).Error.Code);
    }
}
