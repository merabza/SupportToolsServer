using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.CommandRequests;

public sealed class UpdateOneGitRepoCommandRequest : ICommand
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateOneGitRepoCommandRequest(string gitKey, GitDataDto newRecord)
    {
        GitKey = gitKey;
        NewRecord = newRecord;
    }

    public string GitKey { get; }
    public GitDataDto NewRecord { get; }
}