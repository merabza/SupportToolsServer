using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using RepositoriesDom;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Repositories.Gits;

public interface IGitsQueriesRepository : IAbstractRepository
{
    //Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default);
    //Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default);
    Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default);
    Task<OneOf<GitDataDto, Err[]>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken);
}