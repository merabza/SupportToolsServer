using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class ReactAppTemplateRepositoryTests : IAsyncLifetime
{
    private readonly ReactAppTemplate _reactAppTemplate = TestData.NewReactAppTemplate("ReduxApp", "redux-typescript");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.ReactAppTemplates.AddRange(_reactAppTemplate, TestData.NewReactAppTemplate("TypeScriptApp"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<ReactAppTemplate> Read(SupportToolsServerDbContext context, string name)
    {
        ReactAppTemplate? reactAppTemplate =
            await new ReactAppTemplateRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<ReactAppTemplate>(reactAppTemplate);
    }

    private async Task<ReactAppTemplate?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.ReactAppTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredReactAppTemplateWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<ReactAppTemplate> all = await new ReactAppTemplateRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["ReduxApp", "TypeScriptApp"], all.Select(x => x.Name).Order());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("ReduxApp")]
    [InlineData("REDUXAPP")]
    [InlineData("Reduxapp")]
    public async Task GetByName_FindsTheNameWithoutCaseAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        ReactAppTemplate found = await Read(context, name);

        Assert.Equal(_reactAppTemplate.Id, found.Id);
        Assert.Equal("ReduxApp", found.Name);
        Assert.Equal("redux-typescript", found.Template);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        ReactAppTemplate? found =
            await new ReactAppTemplateRepository(context).GetByName("VueApp", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Add_StoresTheReactAppTemplateWithItsFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ReactAppTemplateRepository(context).Add(ReactAppTemplate.Create("VueApp", "cra-template-vue"));
            await context.SaveChangesAsync();
        }

        ReactAppTemplate? stored = await Stored("VueApp");
        Assert.NotNull(stored);
        Assert.Equal("cra-template-vue", stored.Template);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new ReactAppTemplateRepository(context).Add(ReactAppTemplate.Create("ReduxApp", "Other"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadReactAppTemplateWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            ReactAppTemplate read = await Read(context, "ReduxApp");
            read.Update("REDUXAPP", "cra-template-redux-typescript");
            new ReactAppTemplateRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        ReactAppTemplate? stored = await Stored("REDUXAPP");
        Assert.NotNull(stored);
        Assert.Equal(_reactAppTemplate.Id, stored.Id);
        Assert.Equal("cra-template-redux-typescript", stored.Template);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheReactAppTemplateChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        ReactAppTemplate readFirst = await Read(first, "ReduxApp");
        ReactAppTemplate readSecond = await Read(second, "ReduxApp");
        readFirst.Update("ReduxApp", "First");
        new ReactAppTemplateRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("ReduxApp", "Second");
        new ReactAppTemplateRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        ReactAppTemplate? stored = await Stored("ReduxApp");
        Assert.NotNull(stored);
        Assert.Equal("First", stored.Template);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheReactAppTemplateOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new ReactAppTemplateRepository(context).Delete(await Read(context, "ReduxApp"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["TypeScriptApp"], await check.ReactAppTemplates.Select(x => x.Name).ToListAsync());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheReactAppTemplateChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        ReactAppTemplate readFirst = await Read(first, "ReduxApp");
        ReactAppTemplate readSecond = await Read(second, "ReduxApp");
        readFirst.Update("ReduxApp", "First");
        new ReactAppTemplateRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new ReactAppTemplateRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("ReduxApp"));
    }
}
