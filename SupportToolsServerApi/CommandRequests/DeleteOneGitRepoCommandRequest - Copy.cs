using MediatRMessagingAbstractions;

namespace SupportToolsServerApi.CommandRequests;

public sealed class DeleteGitIgnoreFileTypeRequestCommand : ICommand
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteGitIgnoreFileTypeRequestCommand(string recordKey)
    {
        RecordKey = recordKey;
    }

    public string RecordKey { get; }
}