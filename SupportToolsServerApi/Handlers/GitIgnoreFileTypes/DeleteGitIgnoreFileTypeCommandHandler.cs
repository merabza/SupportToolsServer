using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Delete;
using SystemTools.MediatRMessagingAbstractions;
using SystemTools.SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitIgnoreFileTypes;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DeleteGitIgnoreFileTypeCommandHandler : ICommandHandler<DeleteGitIgnoreFileTypeRequestCommand>
{
    private readonly GitIgnoreFileTypeDeleteService _gitIgnoreFileTypeDeleteService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteGitIgnoreFileTypeCommandHandler(GitIgnoreFileTypeDeleteService gitIgnoreFileTypeDeleteService)
    {
        _gitIgnoreFileTypeDeleteService = gitIgnoreFileTypeDeleteService;
    }

    public Task<OneOf<Unit, Error[]>> Handle(DeleteGitIgnoreFileTypeRequestCommand request,
        CancellationToken cancellationToken)
    {
        // Assumes request.RecordKey exists; adjust as needed
        return _gitIgnoreFileTypeDeleteService.DeleteGitIgnoreFileType(request.RecordKey, cancellationToken);
    }
}
