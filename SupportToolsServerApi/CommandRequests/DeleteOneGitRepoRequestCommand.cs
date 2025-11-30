using MediatRMessagingAbstractions;

namespace SupportToolsServerApi.CommandRequests;

public sealed class DeleteOneGitRepoRequestCommand : ICommand
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteOneGitRepoRequestCommand(string gitKey)
    {
        GitKey = gitKey;
    }

    public string GitKey { get; }
}