using MediatR;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerDom;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers;

public class UploadGitReposCommandHandler : ICommandHandler<UploadGitReposCommandRequest>
{
    private IGitsRepository _gitsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UploadGitReposCommandHandler(IGitsRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public Task<OneOf<Unit, IEnumerable<Err>>> Handle(UploadGitReposCommandRequest request, CancellationToken cancellationToken)
    {
        var gitDataUpdater = new GitDataUpdater(request.Gits, request.GitIgnoreFiles, _gitsRepo);
        gitDataUpdater.Run();
    }
}