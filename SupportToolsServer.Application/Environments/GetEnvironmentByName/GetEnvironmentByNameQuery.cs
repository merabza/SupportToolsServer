using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Environments.GetEnvironmentByName;

public sealed class GetEnvironmentByNameQuery : IQuery<StsEnvironmentDataModel>
{
    public GetEnvironmentByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
