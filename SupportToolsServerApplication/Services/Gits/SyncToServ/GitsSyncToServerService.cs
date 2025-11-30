using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf;
using SupportToolsServerApplication.Repositories.Gits;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemToolsShared.Errors;
using WebInstallers;

namespace SupportToolsServerApplication.Services.Gits.SyncToServ;

// ReSharper disable once UnusedType.Global
public class GitsSyncToServerService : IScopedService
{
    private readonly IGitsCommandsRepository _gitsCommandsRepo;

    public GitsSyncToServerService(IGitsCommandsRepository gitsCommandsRepo)
    {
        _gitsCommandsRepo = gitsCommandsRepo;
    }

    public async Task<OneOf<Unit, Err[]>> SyncGitsToServer(IEnumerable<GitDataForSave> requestGits,
        CancellationToken cancellationToken = default)
    {
        foreach (var git in requestGits)
            await _gitsCommandsRepo.UpdateGitRepo(git, cancellationToken);
        return Unit.Value;
    }
}