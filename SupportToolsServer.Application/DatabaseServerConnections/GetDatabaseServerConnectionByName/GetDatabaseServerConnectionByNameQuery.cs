using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnectionByName;

public sealed class GetDatabaseServerConnectionByNameQuery : IQuery<StsDatabaseServerConnectionDataModel>
{
    public GetDatabaseServerConnectionByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
