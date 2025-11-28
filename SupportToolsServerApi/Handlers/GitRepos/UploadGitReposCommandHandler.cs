using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerDom;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitRepos;

public sealed class UploadGitReposCommandHandler : ICommandHandler<UploadGitReposCommandRequest>
{
    private readonly IGitsCommandsRepository _gitsCommandsRepo;
    private readonly IGitsQueriesRepository _gitsQueriesRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UploadGitReposCommandHandler(IGitsCommandsRepository gitsRepo, IGitsQueriesRepository gitsQueriesRepo)
    {
        _gitsCommandsRepo = gitsRepo;
        _gitsQueriesRepo = gitsQueriesRepo;
    }

    public async Task<OneOf<Unit, Err[]>> Handle(UploadGitReposCommandRequest request,
        CancellationToken cancellationToken)
    {
        var gitDataUpdater =
            new GitDataUpdater(request.Gits, request.GitIgnoreFiles, _gitsQueriesRepo, _gitsCommandsRepo);
        await gitDataUpdater.Run(cancellationToken);
        return Unit.Value;
    }
}