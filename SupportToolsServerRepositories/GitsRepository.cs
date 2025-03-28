﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerDb;
using SupportToolsServerDb.Models;
using SupportToolsServerDom;

namespace SupportToolsServerRepositories;

public class GitsRepository : IGitsRepository
{
    private readonly SupportToolsServerDbContext _dbContext;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitsRepository(SupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default)
    {
        return _dbContext.GitData.ToListAsync(cancellationToken);
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
}