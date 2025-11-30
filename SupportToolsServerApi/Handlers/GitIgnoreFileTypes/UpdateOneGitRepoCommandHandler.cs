using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Update;
using SupportToolsServerMappers;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitIgnoreFileTypes;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class UpdateGitIgnoreFileTypeCommandHandler : ICommandHandler<UpdateGitIgnoreFileTypeRequestCommand>
{
    private readonly GitIgnoreFileTypeUpdateService _updateService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateGitIgnoreFileTypeCommandHandler(GitIgnoreFileTypeUpdateService updateService)
    {
        _updateService = updateService;
    }

    public async Task<OneOf<Unit, Err[]>> Handle(UpdateGitIgnoreFileTypeRequestCommand request,
        CancellationToken cancellationToken)
    {
        // Assumes request.RecordKey exists; adjust as needed
        await _updateService.UpdateGitIgnoreFileType(request.NewRecord.AdaptTo(), cancellationToken);
        return Unit.Value;
    }
}