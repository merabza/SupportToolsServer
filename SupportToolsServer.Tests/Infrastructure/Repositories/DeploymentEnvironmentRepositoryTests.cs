using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class DeploymentEnvironmentRepositoryTests : IAsyncLifetime
{
    private readonly DeploymentEnvironment _prod = TestData.NewEnvironment("Prod", "Production");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.Environments.AddRange(_prod, TestData.NewEnvironment("Dev"));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    private static async Task<DeploymentEnvironment> Read(SupportToolsServerDbContext context, string name)
    {
        DeploymentEnvironment? environment =
            await new DeploymentEnvironmentRepository(context).GetByName(name, CancellationToken.None);
        return Assert.IsType<DeploymentEnvironment>(environment);
    }

    private async Task<DeploymentEnvironment?> Stored(string name)
    {
        await using SupportToolsServerDbContext check = _database.NewContext();
        return await check.Environments.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name);
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredEnvironmentWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<DeploymentEnvironment> all =
            await new DeploymentEnvironmentRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["Dev", "Prod"], all.Select(x => x.Name).Order());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    //SQLite compares with case, so a case-insensitive result proves that the repository does not depend on the
    //collation of the database
    [Theory]
    [InlineData("Prod")]
    [InlineData("prod")]
    [InlineData("PROD")]
    public async Task GetByName_FindsTheNameWithoutCaseAndWithoutTrackingIt(string name)
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        DeploymentEnvironment found = await Read(context, name);

        Assert.Equal(_prod.Id, found.Id);
        Assert.Equal("Prod", found.Name);
        Assert.Equal("Production", found.Description);
        Assert.Equal(1, found.Version);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        DeploymentEnvironment? found =
            await new DeploymentEnvironmentRepository(context).GetByName("Stage", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Add_StoresTheEnvironmentWithItsFirstVersionOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new DeploymentEnvironmentRepository(context).Add(DeploymentEnvironment.Create("Stage", null));
            await context.SaveChangesAsync();
        }

        DeploymentEnvironment? stored = await Stored("Stage");
        Assert.NotNull(stored);
        Assert.Null(stored.Description);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new DeploymentEnvironmentRepository(context).Add(DeploymentEnvironment.Create("Prod", "Other"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadEnvironmentWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            DeploymentEnvironment read = await Read(context, "Prod");
            read.Update("PROD", "Live");
            new DeploymentEnvironmentRepository(context).Update(read);
            await context.SaveChangesAsync();
        }

        DeploymentEnvironment? stored = await Stored("PROD");
        Assert.NotNull(stored);
        Assert.Equal(_prod.Id, stored.Id);
        Assert.Equal("Live", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: the update of a stale read writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheEnvironmentChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        DeploymentEnvironment readFirst = await Read(first, "Prod");
        DeploymentEnvironment readSecond = await Read(second, "Prod");
        readFirst.Update("Prod", "First");
        new DeploymentEnvironmentRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("Prod", "Second");
        new DeploymentEnvironmentRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        DeploymentEnvironment? stored = await Stored("Prod");
        Assert.NotNull(stored);
        Assert.Equal("First", stored.Description);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Delete_RemovesTheEnvironmentOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new DeploymentEnvironmentRepository(context).Delete(await Read(context, "Prod"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["Dev"], await check.Environments.Select(x => x.Name).ToListAsync());
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheEnvironmentChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        DeploymentEnvironment readFirst = await Read(first, "Prod");
        DeploymentEnvironment readSecond = await Read(second, "Prod");
        readFirst.Update("Prod", "First");
        new DeploymentEnvironmentRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new DeploymentEnvironmentRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        Assert.NotNull(await Stored("Prod"));
    }
}
