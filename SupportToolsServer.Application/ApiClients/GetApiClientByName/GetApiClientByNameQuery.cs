using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ApiClients.GetApiClientByName;

public sealed class GetApiClientByNameQuery : IQuery<StsApiClientDataModel>
{
    public GetApiClientByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
