using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.QueryRequests;

public sealed class GetOneGitRepoQueryRequest : IQuery<GitDataDto>
{
    public string GitKey { get; }

    public GetOneGitRepoQueryRequest(string gitKey)
    {
        GitKey = gitKey;
    }
}