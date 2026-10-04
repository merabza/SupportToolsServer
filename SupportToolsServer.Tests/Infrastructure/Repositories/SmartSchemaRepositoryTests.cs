using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.SmartSchemas;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class SmartSchemaRepositoryTests : IAsyncLifetime
{
    private readonly SmartSchema _reduce = TestData.NewSmartSchema("Reduce", 1, [("Day", 2), ("Month", 1)]);
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.SmartSchemas.AddRange(_reduce, TestData.NewSmartSchema("Hourly", 1, [("Hour", 48)]));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<SmartSchema> Read(SupportToolsServerDbContext context, string name)
    {
        SmartSchema? smartSchema = await new SmartSchemaRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<SmartSchema>(smartSchema);
    }

    private static async Task<SmartSchema> ReadForUpdate(SupportToolsServerDbContext context, string name)
    {
        SmartSchema? smartSchema =
            await new SmartSchemaRepository(context).GetByNameForUpdate(name, CancellationToken.None);
        return Assert.IsType<SmartSchema>(smartSchema);
    }

    private async Task<SmartSchema?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.SmartSchemas.AsNoTracking().Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Name == name);
    }

    //Every detail row of the database, as "period type=preserve count", so that orphans would show
    private async Task<List<string>> StoredDetailRows()
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.Set<SmartSchemaDetail>().AsNoTracking()
            .OrderBy(x => x.PeriodType).Select(x => x.PeriodType + "=" + x.PreserveCount).ToListAsync();
    }

    private static List<string> DetailsOf(SmartSchema smartSchema)
    {
        return [.. smartSchema.Details.Select(x => x.PeriodType + "=" + x.PreserveCount).Order()];
    }

    [Fact]
    public async Task GetAll_ReturnsEverySchemaWithItsDetailsWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<SmartSchema> all = await new SmartSchemaRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["Hourly", "Reduce"], all.Select(x => x.Name).Order());
        Assert.Equal(["Day=2", "Month=1"], DetailsOf(all.Single(x => x.Name == "Reduce")));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("Reduce")]
    [InlineData("reduce")]
    [InlineData("REDUCE")]
    public async Task GetByName_FindsTheNameWithoutCaseWithTheDetailsAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        SmartSchema found = await Read(context, name);

        Assert.Equal(_reduce.Id, found.Id);
        Assert.Equal("Reduce", found.Name);
        Assert.Equal(1, found.LastPreserveCount);
        Assert.Equal(["Day=2", "Month=1"], DetailsOf(found));
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new SmartSchemaRepository(context).GetByName("Daily", CancellationToken.None));
    }

    //Only the schema that is updated and its details are tracked
    [Theory]
    [InlineData("Reduce")]
    [InlineData("REDUCE")]
    public async Task GetByNameForUpdate_FindsTheNameWithoutCaseAndTracksOnlyThatSchemaWithItsDetails(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        SmartSchema found = await ReadForUpdate(context, name);

        Assert.Equal(_reduce.Id, found.Id);
        Assert.Equal(["Day=2", "Month=1"], DetailsOf(found));
        Assert.Equal(3, context.ChangeTracker.Entries().Count());
        Assert.Equal(EntityState.Unchanged, context.Entry(found).State);
    }

    [Fact]
    public async Task GetByNameForUpdate_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        Assert.Null(await new SmartSchemaRepository(context).GetByNameForUpdate("Daily", CancellationToken.None));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Add_StoresTheSchemaWithItsDetailsAndTheFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new SmartSchemaRepository(context).Add(SmartSchema.Create("Daily", 3,
                [SmartSchemaDetail.Create("Day", 7), SmartSchemaDetail.Create("Week", 4)]));
            await context.SaveChangesAsync();
        }

        SmartSchema? stored = await Stored("Daily");
        Assert.NotNull(stored);
        Assert.Equal(3, stored.LastPreserveCount);
        Assert.Equal(["Day=7", "Week=4"], DetailsOf(stored));
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new SmartSchemaRepository(context).Add(SmartSchema.Create("Reduce", 1, []));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The unique index of the details keeps one detail of a period type per schema
    [Fact]
    public async Task Add_IsRefusedOnSave_WhenAPeriodTypeRepeatsInTheSchema()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new SmartSchemaRepository(context).Add(SmartSchema.Create("Daily", 1,
            [SmartSchemaDetail.Create("Day", 1), SmartSchemaDetail.Create("Day", 2)]));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    //The update replaces the details: the replaced rows are deleted and no orphan is left, even for a detail of the
    //same period type, whose old row goes before the new one is inserted
    [Fact]
    public async Task Update_ReplacesTheDetailsOfTheReadSchemaAndStoresItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            SmartSchema read = await ReadForUpdate(context, "Reduce");
            read.Update("REDUCE", 4, [SmartSchemaDetail.Create("Day", 5), SmartSchemaDetail.Create("Year", 1)]);
            new SmartSchemaRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        SmartSchema? stored = await Stored("REDUCE");
        Assert.NotNull(stored);
        Assert.Equal(_reduce.Id, stored.Id);
        Assert.Equal(4, stored.LastPreserveCount);
        Assert.Equal(["Day=5", "Year=1"], DetailsOf(stored));
        Assert.Equal(2, stored.Version);
        Assert.Equal(["Day=5", "Hour=48", "Year=1"], await StoredDetailRows());
    }

    [Fact]
    public async Task Update_RemovesEveryDetail_WhenTheSchemaHasNoneLeft()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            SmartSchema read = await ReadForUpdate(context, "Reduce");
            read.Update("Reduce", 1, []);
            new SmartSchemaRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        SmartSchema? stored = await Stored("Reduce");
        Assert.NotNull(stored);
        Assert.Empty(stored.Details);
        Assert.Equal(["Hour=48"], await StoredDetailRows());
    }

    //The version is the concurrency token: the update of a stale read writes nothing, neither the schema nor its
    //details
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheSchemaChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        SmartSchema readFirst = await ReadForUpdate(first, "Reduce");
        SmartSchema readSecond = await ReadForUpdate(second, "Reduce");
        readFirst.Update("Reduce", 2, [SmartSchemaDetail.Create("Week", 3)]);
        new SmartSchemaRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("Reduce", 9, [SmartSchemaDetail.Create("Day", 9)]);
        new SmartSchemaRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        SmartSchema? stored = await Stored("Reduce");
        Assert.NotNull(stored);
        Assert.Equal(2, stored.LastPreserveCount);
        Assert.Equal(["Week=3"], DetailsOf(stored));
        Assert.Equal(2, stored.Version);
        Assert.Equal(["Hour=48", "Week=3"], await StoredDetailRows());
    }

    //Update relies on the one version increment of SmartSchema.Update: it expects the stored version to be one less
    //than the version of the instance, so an instance whose version did not grow is refused
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheVersionOfTheReadSchemaDidNotGrow()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        SmartSchema read = await ReadForUpdate(context, "Reduce");
        new SmartSchemaRepository(context).Update(read);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());

        SmartSchema? stored = await Stored("Reduce");
        Assert.NotNull(stored);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheSchemaWithItsDetailsOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new SmartSchemaRepository(context).Delete(await Read(context, "Reduce"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["Hourly"], await check.SmartSchemas.Select(x => x.Name).ToListAsync());
        Assert.Equal(["Hour=48"], await StoredDetailRows());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheSchemaChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        SmartSchema readFirst = await ReadForUpdate(first, "Reduce");
        SmartSchema readSecond = await Read(second, "Reduce");
        readFirst.Update("Reduce", 2, [SmartSchemaDetail.Create("Week", 3)]);
        new SmartSchemaRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new SmartSchemaRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("Reduce"));
        Assert.Equal(["Hour=48", "Week=3"], await StoredDetailRows());
    }
}
