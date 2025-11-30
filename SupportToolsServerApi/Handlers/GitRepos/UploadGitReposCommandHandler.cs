using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.SyncToServ;
using SupportToolsServerApplication.Services.Gits.SyncToServ;
using SupportToolsServerMappers;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitRepos;

public sealed class UploadGitReposCommandHandler : ICommandHandler<UploadGitReposRequestCommand>
{
    private readonly GitIgnoreFilesSyncToServerService _gitIgnoreFilesSyncToServerService;
    private readonly GitsSyncToServerService _gitsSyncToServerService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UploadGitReposCommandHandler(GitIgnoreFilesSyncToServerService gitIgnoreFilesSyncToServerService,
        GitsSyncToServerService gitsSyncToServerService)
    {
        _gitIgnoreFilesSyncToServerService = gitIgnoreFilesSyncToServerService;
        _gitsSyncToServerService = gitsSyncToServerService;
    }

    public async Task<OneOf<Unit, Err[]>> Handle(UploadGitReposRequestCommand request,
        CancellationToken cancellationToken)
    {
        var syncGitIgnoreFilesToServerResult =
            await _gitIgnoreFilesSyncToServerService.SyncGitIgnoreFilesToServer(
                request.GitIgnoreFiles.Select(s => s.AdaptTo()), cancellationToken);
        if (syncGitIgnoreFilesToServerResult.IsT1)
            return syncGitIgnoreFilesToServerResult.AsT1;
        return await _gitsSyncToServerService.SyncGitsToServer(request.Gits.Select(s => s.AdaptTo()),
            cancellationToken);
    }
}