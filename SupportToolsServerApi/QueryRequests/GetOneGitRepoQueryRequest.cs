using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.QueryRequests;

public sealed class GetOneGitRepoQueryRequest : IQuery<GitDataDto>
{
    public string GitKey { get; }

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetOneGitRepoQueryRequest(string gitKey)
    {
        GitKey = gitKey;
    }
}