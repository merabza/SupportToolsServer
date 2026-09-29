using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class EditorConfigFileTypeRepositoryTests : IAsyncLifetime
{
    private readonly EditorConfigFileType _baGetter = TestData.NewEditorConfigFileType("BaGetter", "root = false");
    private readonly EditorConfigFileType _default = TestData.NewEditorConfigFileType("default", "root = true");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.EditorConfigFileTypes.AddRange(_default, _baGetter);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredTypeWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<EditorConfigFileType> all =
            await new EditorConfigFileTypeRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["BaGetter", "default"], all.Select(x => x.Name).Order());
        Assert.Equal("root = true", all.Single(x => x.Id == _default.Id).Content);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Add_StoresTheTypeOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new EditorConfigFileTypeRepository(context).Add(TestData.NewEditorConfigFileType("React", "[*.ts]"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal("[*.ts]", (await check.EditorConfigFileTypes.SingleAsync(x => x.Name == "React")).Content);
    }

    [Fact]
    public async Task Add_IsRefusedOnSave_WhenTheNameIsAlreadyStored()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();
        new EditorConfigFileTypeRepository(context).Add(TestData.NewEditorConfigFileType("default"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Update_StoresTheValuesOfANewInstanceWithTheSameId()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            var repository = new EditorConfigFileTypeRepository(context);
            await repository.GetAll(CancellationToken.None);
            repository.Update(new EditorConfigFileType(_default.Id, "default", "root = true\n[*.cs]"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal("root = true\n[*.cs]",
            (await check.EditorConfigFileTypes.SingleAsync(x => x.Name == "default")).Content);
    }

    [Fact]
    public async Task Delete_RemovesTheTypeOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            var repository = new EditorConfigFileTypeRepository(context);
            List<EditorConfigFileType> stored = await repository.GetAll(CancellationToken.None);
            repository.Delete(stored.Single(x => x.Id == _baGetter.Id));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["default"], await check.EditorConfigFileTypes.Select(x => x.Name).ToListAsync());
    }
}
