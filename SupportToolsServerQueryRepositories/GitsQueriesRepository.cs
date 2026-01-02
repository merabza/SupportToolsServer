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
using SupportToolsServerApplication.Repositories.Gits;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemToolsShared.Errors;

namespace SupportToolsServerQueryRepositories;

public sealed class GitsQueriesRepository : AbstractRepository, IGitsQueriesRepository
{
    private readonly SupportToolsServerDbContext _dbContext;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitsQueriesRepository(SupportToolsServerDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    //public async Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default)
    //{
    //    return await _dbContext.GitData.ToListAsync(cancellationToken);
    //}

    //public Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default)
    //{
    //    return _dbContext.GitIgnoreFileTypes.ToListAsync(cancellationToken);
    //}

    public Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default)
    {
        //return _dbContext.GitData.Include(i => i.GitIgnoreFileTypeNavigation).Select(s =>
        //    new GitDataDto
        //    {
        //        GitIgnorePathName = s.GitIgnoreFileTypeNavigation.Name,
        //        GitProjectAddress = s.GdGitAddress,
        //        GitProjectFolderName = s.GdFolderName,
        //        GitProjectName = s.GdName
        //    }).ToListAsync(cancellationToken);
        throw new NotImplementedException();
    }

    public async Task<OneOf<GitDataDto, Err[]>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken)
    {
        //var gitData = await _dbContext.GitData.Include(i => i.GitIgnoreFileTypeNavigation)
        //    .FirstOrDefaultAsync(x => x.GdName == gitKey, cancellationToken);

        //if (gitData is null)
        //    return new Err[]
        //    {
        //        new()
        //        {
        //            ErrorCode = "GitWithThisNameNotFound",
        //            ErrorMessage = $"Git With This Name {gitKey} Not Found"
        //        }
        //    };

        //return new GitDataDto
        //{
        //    GitIgnorePathName = gitData.GitIgnoreFileTypeNavigation.Name,
        //    GitProjectAddress = gitData.GdGitAddress,
        //    GitProjectFolderName = gitData.GdFolderName,
        //    GitProjectName = gitData.GdName
        //};
        throw new NotImplementedException();
    }
}