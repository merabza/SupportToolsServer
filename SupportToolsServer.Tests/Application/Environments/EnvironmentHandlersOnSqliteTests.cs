using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.Environments.DeleteEnvironment;
using SupportToolsServer.Application.Environments.UpdateEnvironment;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Environments;

//The handlers with the real repository and unit of work on SQLite, one context per request as in the host.
//The concurrent requests prove that the version check also holds between the read and the save of a handler
public sealed class EnvironmentHandlersOnSqliteTests : IAsyncLifetime
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
        var handler = new UpdateEnvironmentCommandHandler(new DeploymentEnvironmentRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new UpdateEnvironmentCommand(TestData.EnvironmentModel(name, description, version)),
            CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteEnvironmentCommandHandler(new DeploymentEnvironmentRepository(context),
            new ProjectCreatorSettingsRepository(context), new ProjectRepository(context),
            new ServerRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteEnvironmentCommand(name, version), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<DeploymentEnvironment>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.Environments.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Upsert_CreatesTheRecordAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert("Prod", "Production", 0)).Value);
        Assert.Equal(2, (await Upsert("Prod", "Live", 1)).Value);

        DeploymentEnvironment stored = Assert.Single(await Stored());
        Assert.Equal("Live", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one record per name
    [Fact]
    public async Task Upsert_MatchesTheNameWithoutCase()
    {
        await Upsert("Prod", "Production", 0);

        Result<int> created = await Upsert("PROD", "Other", 0);
        Result<int> updated = await Upsert("PROD", "Production", 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        DeploymentEnvironment stored = Assert.Single(await Stored());
        Assert.Equal("PROD", stored.Name);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("Prod", "Production", 0);

        Result<int> result = await Upsert("Prod", "Mine", 1,
            async () => Assert.Equal(2, (await Upsert("Prod", "Theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Environment Prod Version Conflict: Expected 1, Actual 2", result.Error.Description);
        DeploymentEnvironment stored = Assert.Single(await Stored());
        Assert.Equal("Theirs", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("Prod", "Mine", 0,
            async () => Assert.Equal(1, (await Upsert("Prod", "Theirs", 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Environment Prod Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Equal("Theirs", Assert.Single(await Stored()).Description);
    }

    [Fact]
    public async Task Upsert_ReturnsRecordWithNameNotFound_WhenAnotherRequestDeletesBetweenTheReadAndTheSave()
    {
        await Upsert("Prod", "Production", 0);

        Result<int> result =
            await Upsert("Prod", "Mine", 1, async () => Assert.True((await Delete("Prod", 1)).IsSuccess));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordOfTheExpectedVersion_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("Prod", "Production", 0);

        Result deleted = await Delete("prod", 1);
        Result deletedAgain = await Delete("Prod", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert("Prod", "Production", 0);

        Result result = await Delete("Prod", null,
            async () => Assert.Equal(2, (await Upsert("Prod", "Theirs", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Environment Prod Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("Theirs", Assert.Single(await Stored()).Description);
    }
}
