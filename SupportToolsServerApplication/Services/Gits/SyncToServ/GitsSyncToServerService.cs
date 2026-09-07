using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApplication.Repositories.Gits;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Services.Gits.SyncToServ;

// ReSharper disable once UnusedType.Global
public class GitsSyncToServerService : IScopedServiceSupportToolsServerApplication
{
    private readonly IGitsCommandsRepository _gitsCommandsRepo;

    public GitsSyncToServerService(IGitsCommandsRepository gitsCommandsRepo)
    {
        _gitsCommandsRepo = gitsCommandsRepo;
    }

    public async Task<Result> SyncGitsToServer(IEnumerable<GitDataForSave> requestGits,
        CancellationToken cancellationToken = default)
    {
        foreach (GitDataForSave git in requestGits)
        {
            await _gitsCommandsRepo.UpdateGitRepo(git, cancellationToken);
        }

        return Result.Success();
    }
}
