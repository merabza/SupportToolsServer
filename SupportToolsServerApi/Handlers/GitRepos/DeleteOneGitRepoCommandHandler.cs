using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerApplication.Repositories.Gits;
using SupportToolsServerApplication.Services.Gits.Delete;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitRepos;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DeleteOneGitRepoCommandHandler : ICommandHandler<DeleteOneGitRepoRequestCommand>
{
    private readonly GitDeleteService _gitDeleteService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteOneGitRepoCommandHandler(IGitsCommandsRepository gitsRepo, GitDeleteService gitDeleteService)
    {
        _gitDeleteService = gitDeleteService;
    }

    public async Task<OneOf<Unit, Err[]>> Handle(DeleteOneGitRepoRequestCommand request, CancellationToken cancellationToken)
    {
        // Assumes request.RecordKey exists; adjust as needed
        return await _gitDeleteService.DeleteGitRepo(request.GitKey, cancellationToken);
    }
}