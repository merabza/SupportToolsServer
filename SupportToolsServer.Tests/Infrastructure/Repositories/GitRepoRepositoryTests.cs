using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerDbPart.Db;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.Repositories;

public sealed class GitRepoRepositoryTests : IAsyncLifetime
{
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
    private readonly GitIgnoreFileType _react = TestData.NewGitIgnoreFileType("React");
    private SupportToolsServerSqliteDatabase _database = null!;
    private GitRepo _repoA = null!;

    public async Task InitializeAsync()
    {
        _database = await SupportToolsServerSqliteDatabase.CreateAsync();
        _repoA = TestData.NewGitRepo("RepoA", _cSharp);
        await using SupportToolsServerDbContext context = _database.NewContext();
        context.GitIgnoreFileTypes.AddRange(_cSharp, _react);
        context.GitRepos.AddRange(_repoA, TestData.NewGitRepo("RepoB", _react));
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task GetAll_ReturnsEveryStoredGitWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        List<GitRepo> all = await new GitRepoRepository(context).GetAll(CancellationToken.None);

        Assert.Equal(["RepoA", "RepoB"], all.Select(x => x.Name).Order());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsTheStoredGitWithoutTrackingIt()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        GitRepo? found = await new GitRepoRepository(context).GetByName("RepoA", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(_repoA.Id, found.Id);
        Assert.Equal(TestData.AddressOf("RepoA"), found.Address);
        Assert.Equal("RepoA", found.FolderName);
        Assert.Equal(_cSharp.Id, found.GitIgnoreFileTypeId);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetByName_ReturnsNull_WhenThereIsNoSuchName()
    {
        await using SupportToolsServerDbContext context = _database.NewContext();

        GitRepo? found = await new GitRepoRepository(context).GetByName("RepoZ", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Add_StoresTheGitOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            new GitRepoRepository(context).Add(TestData.NewGitRepo("RepoC", _react));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        GitRepo stored = await check.GitRepos.SingleAsync(x => x.Name == "RepoC");
        Assert.Equal(_react.Id, stored.GitIgnoreFileTypeId);
    }

    [Fact]
    public async Task Update_StoresTheValuesOfANewInstanceWithTheSameIdAndTheNextVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            var repository = new GitRepoRepository(context);
            await repository.GetAll(CancellationToken.None);
            repository.Update(new GitRepo(_repoA.Id, "RepoA", "git@github.com:test/moved.git", "Moved", _react.Id,
                _repoA.Version + 1));
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        GitRepo stored = await check.GitRepos.SingleAsync(x => x.Name == "RepoA");
        Assert.Equal("git@github.com:test/moved.git", stored.Address);
        Assert.Equal("Moved", stored.FolderName);
        Assert.Equal(_react.Id, stored.GitIgnoreFileTypeId);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Update_StoresTheDomainUpdateOfTheReadGitWithItsNewVersion()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            var repository = new GitRepoRepository(context);
            GitRepo read = (await repository.GetByName("RepoA", CancellationToken.None))!;
            read.Update("RepoA", read.Address, "Moved", read.GitIgnoreFileTypeId);
            repository.Update(read);
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        GitRepo stored = await check.GitRepos.SingleAsync(x => x.Name == "RepoA");
        Assert.Equal("Moved", stored.FolderName);
        Assert.Equal(2, stored.Version);
    }

    //The version is the concurrency token: an update based on a version that is no longer stored writes nothing
    [Fact]
    public async Task Update_IsRefusedOnSave_WhenTheGitChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        GitRepo readFirst = (await new GitRepoRepository(first).GetByName("RepoA", CancellationToken.None))!;
        GitRepo readSecond = (await new GitRepoRepository(second).GetByName("RepoA", CancellationToken.None))!;
        readFirst.Update("RepoA", readFirst.Address, "First", readFirst.GitIgnoreFileTypeId);
        new GitRepoRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        readSecond.Update("RepoA", readSecond.Address, "Second", readSecond.GitIgnoreFileTypeId);
        new GitRepoRepository(second).Update(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using SupportToolsServerDbContext check = _database.NewContext();
        GitRepo stored = await check.GitRepos.SingleAsync(x => x.Name == "RepoA");
        Assert.Equal("First", stored.FolderName);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public async Task Delete_IsRefusedOnSave_WhenTheGitChangedAfterItWasRead()
    {
        await using SupportToolsServerDbContext first = _database.NewContext();
        await using SupportToolsServerDbContext second = _database.NewContext();
        GitRepo readFirst = (await new GitRepoRepository(first).GetByName("RepoA", CancellationToken.None))!;
        GitRepo readSecond = (await new GitRepoRepository(second).GetByName("RepoA", CancellationToken.None))!;
        readFirst.Update("RepoA", readFirst.Address, "First", readFirst.GitIgnoreFileTypeId);
        new GitRepoRepository(first).Update(readFirst);
        await first.SaveChangesAsync();
        new GitRepoRepository(second).Delete(readSecond);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.True(await check.GitRepos.AnyAsync(x => x.Name == "RepoA"));
    }

    [Fact]
    public async Task Delete_RemovesTheGitOnSave()
    {
        await using (SupportToolsServerDbContext context = _database.NewContext())
        {
            var repository = new GitRepoRepository(context);
            GitRepo? stored = await repository.GetByName("RepoB", CancellationToken.None);
            repository.Delete(stored!);
            await context.SaveChangesAsync();
        }

        await using SupportToolsServerDbContext check = _database.NewContext();
        Assert.Equal(["RepoA"], await check.GitRepos.Select(x => x.Name).ToListAsync());
    }
}
