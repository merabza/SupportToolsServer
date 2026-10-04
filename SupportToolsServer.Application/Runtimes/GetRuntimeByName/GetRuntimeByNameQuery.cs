using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Runtimes.GetRuntimeByName;

public sealed class GetRuntimeByNameQuery : IQuery<StsRuntimeDataModel>
{
    public GetRuntimeByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
