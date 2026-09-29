using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class GitIgnoreFileTypeRepositoryTests : IAsyncLifetime
{
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp", "bin/");
    private SupportToolsServerSqliteDatabase _database = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.GitIgnoreFileTypes.AddRange(_cSharp, TestData.NewGitIgnoreFileType("React", "node_modules/"));
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

        List<GitIgnoreFileType> all = await new GitIgnoreFileTypeRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["CSharp", "React"], all.Select(x => x.Name).Order());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsTheStoredTypeWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        GitIgnoreFileType? found =
            await new GitIgnoreFileTypeRepository(context).GetByName("CSharp", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(_cSharp.Id, found.Id);
        Assert.Equal("bin/", found.Content);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        GitIgnoreFileType? found =
            await new GitIgnoreFileTypeRepository(context).GetByName("Python", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Add_StoresTheTypeOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new GitIgnoreFileTypeRepository(context).Add(TestData.NewGitIgnoreFileType("Python", "venv/"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal("venv/", (await check.GitIgnoreFileTypes.SingleAsync(x => x.Name == "Python")).Content);
    }

    [Fact]
    public async Task Update_StoresTheValuesOfANewInstanceWithTheSameId()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            var repository = new GitIgnoreFileTypeRepository(context);
            await repository.GetAll(CancellationToken.None);
            repository.Update(new GitIgnoreFileType(_cSharp.Id, "CSharp", "bin/\nobj/"));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal("bin/\nobj/", (await check.GitIgnoreFileTypes.SingleAsync(x => x.Name == "CSharp")).Content);
    }

    [Fact]
    public async Task Delete_RemovesTheTypeOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            var repository = new GitIgnoreFileTypeRepository(context);
            GitIgnoreFileType? stored = await repository.GetByName("React", CancellationToken.None);
            repository.Delete(stored!);
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["CSharp"], await check.GitIgnoreFileTypes.Select(x => x.Name).ToListAsync());
    }
}
