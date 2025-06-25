using MediatRMessagingAbstractions;

namespace SupportToolsServerApi.CommandRequests;

public sealed class DeleteOneGitRepoCommandRequest : ICommand
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteOneGitRepoCommandRequest(string gitKey)
    {
        GitKey = gitKey;
    }

    public string GitKey { get; }
}