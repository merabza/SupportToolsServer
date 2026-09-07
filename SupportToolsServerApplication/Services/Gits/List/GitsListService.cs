using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApplication.Repositories.Gits;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Services.Gits.List;

public class GitsListService : IScopedServiceSupportToolsServerApplication
{
    private readonly IGitsQueriesRepository _repo;

    public GitsListService(IGitsQueriesRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<List<GitDataDto>>> GetGits(CancellationToken cancellationToken)
    {
        return await _repo.GetGitRepos(cancellationToken);
    }

    public async Task<Result<GitDataDto>> GetOneGit(string requestGitKey, CancellationToken cancellationToken)
    {
        return await _repo.GetGitRepoByKey(requestGitKey, cancellationToken);
    }
}
