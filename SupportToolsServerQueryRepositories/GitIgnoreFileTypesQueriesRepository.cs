using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneOf;
using RepositoriesDom;
using SupportToolsServer.Domain.GitIgnoreFileTypes;
using SupportToolsServer.Persistence;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemToolsShared.Errors;

namespace SupportToolsServerQueryRepositories;

public sealed class GitIgnoreFileTypesQueriesRepository : AbstractRepository, IGitIgnoreFileTypesQueriesRepository
{
    private readonly SupportToolsServerDbContext _dbContext;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitIgnoreFileTypesQueriesRepository(SupportToolsServerDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OneOf<List<GitIgnoreFileTypeDto>, Err[]>> GetGitIgnoreFileTypes(
        CancellationToken cancellationToken)
    {
        //return await _dbContext.GitIgnoreFileTypes.Select(s =>
        //    new GitIgnoreFileTypeDto
        //    {
        //        RowId = s.RowId,
        //        Name = s.Name,
        //        Content = s.Content
        //    }).ToListAsync(cancellationToken);
        throw new NotImplementedException();
    }

    public async Task<OneOf<List<string>, Err[]>> GetGitIgnoreFileTypeNames(CancellationToken cancellationToken)
    {
        return await _dbContext.GitIgnoreFileTypes.Select(s => s.Name).ToListAsync(cancellationToken);
    }

    public Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default)
    {
        return _dbContext.GitIgnoreFileTypes.ToListAsync(cancellationToken);
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

    //public Task<List<StsGitDataModel>> GetGitRepos(CancellationToken cancellationToken = default)
    //{
    //    return _dbContext.GitData.Include(i => i.GitIgnoreFileTypeNavigation).Select(s =>
    //        new StsGitDataModel
    //        {
    //            GitIgnorePathName = s.GitIgnoreFileTypeNavigation.Name,
    //            GitProjectAddress = s.GdGitAddress,
    //            GitProjectFolderName = s.GdFolderName,
    //            GitProjectName = s.GdName
    //        }).ToListAsync(cancellationToken);
    //}

    //public async Task<OneOf<StsGitDataModel, Err[]>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken)
    //{
    //    var gitData = await _dbContext.GitData.Include(i => i.GitIgnoreFileTypeNavigation)
    //        .FirstOrDefaultAsync(x => x.GdName == gitKey, cancellationToken);

    //    if (gitData is null)
    //        return new Err[]
    //        {
    //            new()
    //            {
    //                ErrorCode = "GitWithThisNameNotFound",
    //                ErrorMessage = $"Git With This Name {gitKey} Not Found"
    //            }
    //        };

    //    return new StsGitDataModel
    //    {
    //        GitIgnorePathName = gitData.GitIgnoreFileTypeNavigation.Name,
    //        GitProjectAddress = gitData.GdGitAddress,
    //        GitProjectFolderName = gitData.GdFolderName,
    //        GitProjectName = gitData.GdName
    //    };
    //}

    //public Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default)
    //{
    //    throw new NotImplementedException();
    //}

    //public async Task<List<GitData>> GetAllGitIgnoreFileTypesFromDb(CancellationToken cancellationToken = default)
    //{
    //    return await _dbContext.GitData.ToListAsync(cancellationToken);
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