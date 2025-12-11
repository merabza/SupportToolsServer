using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using SupportToolsServerApplication.Repositories.Gits;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemToolsShared.Errors;
using WebInstallers;

namespace SupportToolsServerApplication.Services.Gits.List;

public class GitsListService : IScopedService
{
    private readonly IGitsQueriesRepository _repo;

    public GitsListService(IGitsQueriesRepository repo)
    {
        _repo = repo;
    }

    public async Task<OneOf<List<GitDataDto>, Err[]>> GetGits(CancellationToken cancellationToken)
    {
        return await _repo.GetGitRepos(cancellationToken);
    }

    public async Task<OneOf<GitDataDto, Err[]>> GetOneGit(string requestGitKey, CancellationToken cancellationToken)
    {
        return await _repo.GetGitRepoByKey(requestGitKey, cancellationToken);
    }
}