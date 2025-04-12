using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerDom;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers;

public sealed class UploadGitReposCommandHandler : ICommandHandler<UploadGitReposCommandRequest>
{
    private readonly IGitsRepository _gitsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UploadGitReposCommandHandler(IGitsRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public async Task<OneOf<Unit, IEnumerable<Err>>> Handle(UploadGitReposCommandRequest request,
        CancellationToken cancellationToken)
    {
        var gitDataUpdater = new GitDataUpdater(request.Gits, request.GitIgnoreFiles, _gitsRepo);
        await gitDataUpdater.Run(cancellationToken);
        return Unit.Value;
    }
}