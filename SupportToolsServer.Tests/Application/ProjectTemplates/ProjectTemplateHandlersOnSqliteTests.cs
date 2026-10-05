using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.ProjectTemplates.DeleteProjectTemplate;
using SupportToolsServer.Application.ProjectTemplates.GetProjectTemplateByName;
using SupportToolsServer.Application.ProjectTemplates.UpdateProjectTemplate;
using SupportToolsServer.Application.ReactAppTemplates.DeleteReactAppTemplate;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerDbPart.Db;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ProjectTemplates;

//The handlers with the real repositories and unit of work on SQLite, one context per request as in the host.
//Besides the versions, they show the React template reference of both sides: a project template cannot name a
//missing React template, and a React template that a project template uses cannot be deleted
public sealed class ProjectTemplateHandlersOnSqliteTests : IAsyncLifetime
{
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.ReactAppTemplates.AddRange(TestData.NewReactAppTemplate("redux-typescript", "redux-typescript"),
            TestData.NewReactAppTemplate("typescript"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private async Task<Result<int>> Upsert(string name, string? reactTemplateName, int version,
        Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new UpdateProjectTemplateCommandHandler(new ProjectTemplateRepository(context),
            new ReactAppTemplateRepository(context), UnitOfWork(context, concurrentChange));
        return await handler.Handle(
            new UpdateProjectTemplateCommand(TestData.ProjectTemplateModel(name, reactTemplateName, version)),
            CancellationToken.None);
    }

    private async Task<Result> Delete(string name, int? version, Func<Task>? concurrentChange = null)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteProjectTemplateCommandHandler(new ProjectTemplateRepository(context),
            UnitOfWork(context, concurrentChange));
        return await handler.Handle(new DeleteProjectTemplateCommand(name, version), CancellationToken.None);
    }

    private async Task<Result<StsProjectTemplateDataModel>> Get(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new GetProjectTemplateByNameQueryHandler(new ProjectTemplateRepository(context),
            new ReactAppTemplateRepository(context));
        return await handler.Handle(new GetProjectTemplateByNameQuery(name), CancellationToken.None);
    }

    private async Task<Result> DeleteReactAppTemplate(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        var handler = new DeleteReactAppTemplateCommandHandler(new ReactAppTemplateRepository(context),
            new ProjectTemplateRepository(context), new SupportToolsServerUnitOfWork(context));
        return await handler.Handle(new DeleteReactAppTemplateCommand(name, null), CancellationToken.None);
    }

    private static IUnitOfWork UnitOfWork(SupportToolsServerDbContext context, Func<Task>? concurrentChange)
    {
        var unitOfWork = new SupportToolsServerUnitOfWork(context);
        return concurrentChange is null ? unitOfWork : new ConcurrentChangeUnitOfWork(unitOfWork, concurrentChange);
    }

    private async Task<List<ProjectTemplate>> Stored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        return await context.ProjectTemplates.AsNoTracking().ToListAsync();
    }

    //The React template matches without case and is read back with its stored name
    [Fact]
    public async Task Upsert_CreatesTheRecordAndUpdatesItWithTheVersionsItReturns()
    {
        Assert.Equal(1, (await Upsert("Reactredux", "REDUX-TYPESCRIPT", 0)).Value);
        Assert.Equal(2, (await Upsert("Reactredux", "typescript", 1)).Value);

        Assert.Equal(2, Assert.Single(await Stored()).Version);
        StsProjectTemplateDataModel read = (await Get("REACTREDUX")).Value;
        Assert.Equal("Reactredux", read.Name);
        Assert.Equal("Api", read.SupportProjectType);
        Assert.Equal("ReactTest", read.TestProjectName);
        Assert.Equal("Rt", read.TestProjectShortName);
        Assert.True(read.UseDatabase);
        Assert.False(read.UseHttps);
        Assert.Equal("typescript", read.ReactTemplateName);
        Assert.Equal(2, read.Version);
    }

    //The unique index of SQLite is case-sensitive, so only the case-insensitive lookup keeps one record per name
    [Fact]
    public async Task Upsert_MatchesTheNameWithoutCase()
    {
        await Upsert("Console With Database", null, 0);

        Result<int> created = await Upsert("CONSOLE WITH DATABASE", null, 0);
        Result<int> updated = await Upsert("CONSOLE WITH DATABASE", null, 1);

        Assert.Equal("ConcurrencyConflict", created.Error.Code);
        Assert.Equal(2, updated.Value);
        Assert.Equal("CONSOLE WITH DATABASE", Assert.Single(await Stored()).Name);
    }

    [Fact]
    public async Task Upsert_ReturnsReferencedRecordsNotFound_WhenTheReactTemplateIsMissing()
    {
        Result<int> result = await Upsert("Vue", "vue", 0);

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal("Referenced ReactAppTemplate Records Not Found: vue", result.Error.Description);
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBetweenTheReadAndTheSave()
    {
        await Upsert("Reactredux", null, 0);

        Result<int> result = await Upsert("Reactredux", "typescript", 1,
            async () => Assert.Equal(2, (await Upsert("Reactredux", "redux-typescript", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ProjectTemplate Reactredux Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Equal("redux-typescript", (await Get("Reactredux")).Value.ReactTemplateName);
    }

    [Fact]
    public async Task Upsert_ReturnsConcurrencyConflict_WhenAnotherRequestCreatesTheNameBetweenTheReadAndTheSave()
    {
        Result<int> result = await Upsert("Reactredux", "typescript", 0,
            async () => Assert.Equal(1, (await Upsert("Reactredux", null, 0)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ProjectTemplate Reactredux Version Conflict: Expected 0, Actual 1", result.Error.Description);
        Assert.Null(Assert.Single(await Stored()).ReactTemplateId);
    }

    //The foreign key keeps the React template that was deleted after the check; the version still matches, so the
    //save fails with the exception of the database (CLAUDE.md, Registry conventions: a 500)
    [Fact]
    public async Task Upsert_Throws_WhenAnotherRequestDeletesTheReactTemplateBetweenTheCheckAndTheSave()
    {
        await Assert.ThrowsAsync<DbUpdateException>(() => Upsert("Reactredux", "typescript", 0,
            async () => Assert.True((await DeleteReactAppTemplate("typescript")).IsSuccess)));

        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task Delete_RemovesTheRecordOfTheExpectedVersion_AndThenReturnsRecordWithNameNotFound()
    {
        await Upsert("Reactredux", "typescript", 0);

        Result deleted = await Delete("REACTREDUX", 1);
        Result deletedAgain = await Delete("Reactredux", 1);

        Assert.True(deleted.IsSuccess);
        Assert.Empty(await Stored());
        Assert.Equal("RecordWithNameNotFound", deletedAgain.Error.Code);
    }

    [Fact]
    public async Task Delete_WithoutVersion_ReturnsConcurrencyConflict_WhenAnotherRequestUpdatesBeforeTheSave()
    {
        await Upsert("Reactredux", null, 0);

        Result result = await Delete("Reactredux", null,
            async () => Assert.Equal(2, (await Upsert("Reactredux", "typescript", 1)).Value));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ProjectTemplate Reactredux Version Conflict: Expected 1, Actual 2", result.Error.Description);
        Assert.Single(await Stored());
    }

    //The React template stays while a project template uses it, and can be deleted once none does
    [Fact]
    public async Task DeleteReactAppTemplate_ReturnsRecordIsInUse_UntilNoProjectTemplateUsesIt()
    {
        await Upsert("Reactredux", "redux-typescript", 0);
        await Upsert("admin", "REDUX-TYPESCRIPT", 0);

        Result refused = await DeleteReactAppTemplate("Redux-TypeScript");
        await Upsert("Reactredux", null, 1);
        await Delete("admin", 1);
        Result deleted = await DeleteReactAppTemplate("redux-typescript");

        Assert.Equal("RecordIsInUse", refused.Error.Code);
        Assert.Equal("ReactAppTemplate Redux-TypeScript Is Used By: ProjectTemplate admin, ProjectTemplate Reactredux",
            refused.Error.Description);
        Assert.True(deleted.IsSuccess);
    }
}
