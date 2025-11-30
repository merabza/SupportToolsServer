using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.QueryRequests;

public sealed class GetOneGitRepoRequestQuery : IQuery<StsGitDataModel>
{
    public string GitKey { get; }

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetOneGitRepoRequestQuery(string gitKey)
    {
        GitKey = gitKey;
    }
}