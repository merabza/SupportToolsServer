using MediatRMessagingAbstractions;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServerApi.QueryRequests;

public sealed class GetOneGitRepoRequestQuery : IQuery<StsGitDataModel>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public GetOneGitRepoRequestQuery(string gitKey)
    {
        GitKey = gitKey;
    }

    public string GitKey { get; }
}