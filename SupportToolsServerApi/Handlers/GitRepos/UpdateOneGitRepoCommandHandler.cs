using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerDom;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitRepos;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class UpdateOneGitRepoCommandHandler : ICommandHandler<UpdateOneGitRepoCommandRequest>
{
    private readonly IGitsCommandsRepository _gitsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateOneGitRepoCommandHandler(IGitsCommandsRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public async Task<OneOf<Unit, Err[]>> Handle(UpdateOneGitRepoCommandRequest request, CancellationToken cancellationToken)
    {
        // Assumes request.RecordKey exists; adjust as needed
        await _gitsRepo.UpdateGitRepo(request.GitKey, request.NewRecord, cancellationToken);
        return Unit.Value;
    }
}