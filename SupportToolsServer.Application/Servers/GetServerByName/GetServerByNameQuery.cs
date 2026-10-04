using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Servers.GetServerByName;

public sealed class GetServerByNameQuery : IQuery<StsServerDataModel>
{
    public GetServerByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
