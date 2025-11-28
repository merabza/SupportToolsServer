using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RepositoriesDom;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerDb;
using SupportToolsServerDb.Models;
using SupportToolsServerDom;

namespace SupportToolsServerRepositories;

public sealed class GitIgnoreFileTypesCommandsRepository : AbstractRepository, IGitIgnoreFileTypesCommandsRepository
{
    private readonly SupportToolsServerDbContext _dbContext;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitIgnoreFileTypesCommandsRepository(SupportToolsServerDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    //public async Task AddGitIgnorePath(GitIgnoreFileType newGitIgnorePath,
    //    CancellationToken cancellationToken = default)
    //{
    //    await _dbContext.GitIgnoreFileTypes.AddAsync(newGitIgnorePath, cancellationToken);
    //}

    //public void UpdateGitIgnorePath(GitIgnoreFileType dbGitIgnorePath, CancellationToken cancellationToken = default)
    //{
    //    _dbContext.GitIgnoreFileTypes.Update(dbGitIgnorePath);
    //}

    //public async Task AddGit(GitData gitData, CancellationToken cancellationToken = default)
    //{
    //    await _dbContext.GitData.AddAsync(gitData, cancellationToken);
    //}

    //public async Task UpdateGitRepo(string gitKey, GitDataDto requestNewRecord, CancellationToken cancellationToken)
    //{
    //    var gitIgnoreFileType =
    //        await _dbContext.GitIgnoreFileTypes.FirstOrDefaultAsync(x => x.Name == requestNewRecord.GitIgnorePathName,
    //            cancellationToken);

    //    if (gitIgnoreFileType is null)
    //    {
    //        gitIgnoreFileType = new GitIgnoreFileType { Name = requestNewRecord.GitIgnorePathName };
    //        await _dbContext.GitIgnoreFileTypes.AddAsync(gitIgnoreFileType, cancellationToken);
    //    }

    //    var gitData = await _dbContext.GitData.FirstOrDefaultAsync(x => x.GdName == gitKey, cancellationToken);

    //    if (gitData is null)
    //    {
    //        var newGitData = new GitData
    //        {
    //            GdName = gitKey,
    //            GdGitAddress = requestNewRecord.GitProjectAddress,
    //            GdFolderName = requestNewRecord.GitProjectFolderName,
    //            GitIgnoreFileTypeNavigation = gitIgnoreFileType
    //        };
    //        await _dbContext.GitData.AddAsync(newGitData, cancellationToken);
    //    }
    //    else
    //    {
    //        gitData.GdGitAddress = requestNewRecord.GitProjectAddress;
    //        gitData.GdFolderName = requestNewRecord.GitProjectFolderName;
    //        gitData.GitIgnoreFileTypeNavigation = gitIgnoreFileType;
    //        _dbContext.GitData.Update(gitData);
    //    }

    //    await _dbContext.SaveChangesAsync(cancellationToken);
    //}

    //public async Task DeleteGitRepo(string gitKey, CancellationToken cancellationToken)
    //{
    //    var gitData = await _dbContext.GitData.FirstOrDefaultAsync(x => x.GdName == gitKey, cancellationToken);
    //    if (gitData is null)
    //        return;
    //    _dbContext.GitData.Remove(gitData);
    //    await _dbContext.SaveChangesAsync(cancellationToken);
    //}
}