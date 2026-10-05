using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class ProjectTemplateRepositoryTests : IAsyncLifetime
{
    private readonly ReactAppTemplate _reactTemplate = TestData.NewReactAppTemplate("redux-typescript");
    private SupportToolsServerSqliteDatabase _database = null!;
    private ProjectTemplate _reactredux = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        _reactredux = TestData.NewProjectTemplate("Reactredux", _reactTemplate);
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.ReactAppTemplates.Add(_reactTemplate);
        context.ProjectTemplates.AddRange(_reactredux, TestData.NewProjectTemplate("Console With Database"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<ProjectTemplate> Read(SupportToolsServerDbContext context, string name)
    {
        ProjectTemplate? projectTemplate =
            await new ProjectTemplateRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<ProjectTemplate>(projectTemplate);
    }

    private async Task<ProjectTemplate?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.ProjectTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredTemplateWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<ProjectTemplate> all = await new ProjectTemplateRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["Console With Database", "Reactredux"],
            all.Select(x => x.Name).Order(StringComparer.OrdinalIgnoreCase));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("Reactredux")]
    [InlineData("REACTREDUX")]
    [InlineData("reactRedux")]
    public async Task GetByName_FindsTheNameWithoutCaseWithEveryValueAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        ProjectTemplate found = await Read(context, name);

        Assert.Equal(_reactredux.Id, found.Id);
        Assert.Equal("Reactredux", found.Name);
        Assert.Equal("Api", found.SupportProjectType);
        Assert.Equal("ReactTest", found.TestProjectName);
        Assert.Equal("Rt", found.TestProjectShortName);
        Assert.Equal([true, false, true, false, true, false, true, false, true, false],
        [
            found.UseDatabase, found.UseDbPartFolderForDatabaseProjects, found.UseMenu, found.UseHttps,
            found.UseReact, found.UseCarcass, found.UseIdentity, found.UseReCounter, found.UseSignalR,
            found.UseFluentValidation
        ]);
        Assert.Equal(_reactTemplate.Id, found.ReactTemplateId);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new ProjectTemplateRepository(context).GetByName("Vue", CancellationToken.None));
    }

    [Fact]
    public async Task Add_StoresTheTemplateWithItsFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ProjectTemplateRepository(context).Add(ProjectTemplate.Create("Service", "Razor", null, null, false,
                false, false, true, false, true, false, false, false, true, null));
            await context.SaveChangesAsync();
        }

        ProjectTemplate? stored = await Stored("Service");
        Assert.NotNull(stored);
        Assert.Equal("Razor", stored.SupportProjectType);
        Assert.Null(stored.TestProjectName);
        Assert.True(stored.UseHttps);
        Assert.True(stored.UseCarcass);
        Assert.True(stored.UseFluentValidation);
        Assert.False(stored.UseDatabase);
        Assert.Null(stored.ReactTemplateId);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ProjectTemplateRepository(context).Add(TestData.NewProjectTemplate("Reactredux"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The foreign key refuses a React template that is not stored
    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheReactTemplateIsNotStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ProjectTemplateRepository(context).Add(
            TestData.NewProjectTemplate("Vue", TestData.NewReactAppTemplate("vue")));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadTemplateWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            ProjectTemplate read = await Read(context, "Reactredux");
            read.Update("REACTREDUX", "Razor", "Test", null, false, true, false, true, false, true, false, true, false,
                true, null);
            new ProjectTemplateRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        ProjectTemplate? stored = await Stored("REACTREDUX");
        Assert.NotNull(stored);
        Assert.Equal(_reactredux.Id, stored.Id);
        Assert.Equal("Razor", stored.SupportProjectType);
        Assert.Equal("Test", stored.TestProjectName);
        Assert.Null(stored.TestProjectShortName);
        Assert.Equal([false, true, false, true, false, true, false, true, false, true],
        [
            stored.UseDatabase, stored.UseDbPartFolderForDatabaseProjects, stored.UseMenu, stored.UseHttps,
            stored.UseReact, stored.UseCarcass, stored.UseIdentity, stored.UseReCounter, stored.UseSignalR,
            stored.UseFluentValidation
        ]);
        Assert.Null(stored.ReactTemplateId);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheTemplateChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        ProjectTemplate readFirst = await Read(first, "Reactredux");
        ProjectTemplate readSecond = await Read(second, "Reactredux");
        readFirst.Update("Reactredux", "Api", "first", null, false, false, false, false, false, false, false, false,
            false, false, null);
        new ProjectTemplateRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("Reactredux", "Api", "second", null, false, false, false, false, false, false, false,
            false, false, false, null);
        new ProjectTemplateRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        ProjectTemplate? stored = await Stored("Reactredux");
        Assert.NotNull(stored);
        Assert.Equal("first", stored.TestProjectName);
        Assert.Equal(2, stored.Version);
    }

    //The React template belongs to another aggregate and stays
    [Fact]
    public async Task Delete_RemovesTheTemplateOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ProjectTemplateRepository(context).Delete(await Read(context, "Reactredux"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["Console With Database"], await check.ProjectTemplates.Select(x => x.Name).ToListAsync());
        Assert.Equal(1, await check.ReactAppTemplates.CountAsync());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheTemplateChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        ProjectTemplate readFirst = await Read(first, "Reactredux");
        ProjectTemplate readSecond = await Read(second, "Reactredux");
        readFirst.Update("Reactredux", "Api", "first", null, false, false, false, false, false, false, false, false,
            false, false, null);
        new ProjectTemplateRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new ProjectTemplateRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("Reactredux"));
    }

    //The React template cannot be deleted while a project template uses it
    [Fact]
    public async Task DeletingTheReactTemplate_IsRefusedOnSave()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.ReactAppTemplates.Remove(await context.ReactAppTemplates.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
