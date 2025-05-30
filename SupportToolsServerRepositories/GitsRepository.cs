﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneOf;
using RepositoriesDom;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerDb;
using SupportToolsServerDb.Models;
using SupportToolsServerDom;
using SystemToolsShared.Errors;

namespace SupportToolsServerRepositories;

public sealed class GitsRepository : AbstractRepository, IGitsRepository
{
    private readonly SupportToolsServerDbContext _dbContext;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitsRepository(SupportToolsServerDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default)
    {
        return await _dbContext.GitData.ToListAsync(cancellationToken);
    }

    public Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default)
    {
        return _dbContext.GitIgnoreFileTypes.ToListAsync(cancellationToken);
    }

    public async Task AddGitIgnorePath(GitIgnoreFileType newGitIgnorePath,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.GitIgnoreFileTypes.AddAsync(newGitIgnorePath, cancellationToken);
    }

    public void UpdateGitIgnorePath(GitIgnoreFileType dbGitIgnorePath, CancellationToken cancellationToken = default)
    {
        _dbContext.GitIgnoreFileTypes.Update(dbGitIgnorePath);
    }

    public async Task AddGit(GitData gitData, CancellationToken cancellationToken = default)
    {
        await _dbContext.GitData.AddAsync(gitData, cancellationToken);
    }

    public Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default)
    {
        return _dbContext.GitData.Include(i => i.GitIgnoreFileTypeNavigation).Select(s =>
            new GitDataDto
            {
                GitIgnorePathName = s.GitIgnoreFileTypeNavigation.Name,
                GitProjectAddress = s.GdGitAddress,
                GitProjectFolderName = s.GdFolderName,
                GitProjectName = s.GdName
            }).ToListAsync(cancellationToken);
    }

    public async Task<OneOf<GitDataDto, IEnumerable<Err>>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken)
    {
        var gitData = await _dbContext.GitData.Include(i => i.GitIgnoreFileTypeNavigation)
            .FirstOrDefaultAsync(x => x.GdName == gitKey, cancellationToken);

        if (gitData is null)
            return new List<Err>
            {
                new()
                {
                    ErrorCode = "GitWithThisNameNotFound",
                    ErrorMessage = $"Git With This Name {gitKey} Not Found"
                }
            };

        return new GitDataDto
        {
            GitIgnorePathName = gitData.GitIgnoreFileTypeNavigation.Name,
            GitProjectAddress = gitData.GdGitAddress,
            GitProjectFolderName = gitData.GdFolderName,
            GitProjectName = gitData.GdName
        };
    }
}