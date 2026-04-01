using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemTools.SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Repositories.Gits;

public interface IGitsQueriesRepository
{
    //Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default);
    //Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default);
    Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default);
    Task<OneOf<GitDataDto, Error[]>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken);
}
