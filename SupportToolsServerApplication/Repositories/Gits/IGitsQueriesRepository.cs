using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Repositories.Gits;

public interface IGitsQueriesRepository
{
    //Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default);
    //Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default);
    Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default);
    Task<Result<GitDataDto>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken);
}
