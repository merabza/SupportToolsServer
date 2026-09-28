using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.GetGitRepoByKey;

public class GetGitRepoByKeyQuery : IQuery<StsGitDataModel>
{
    public GetGitRepoByKeyQuery(string key)
    {
        Key = key;
    }

    public string Key { get; }
}
