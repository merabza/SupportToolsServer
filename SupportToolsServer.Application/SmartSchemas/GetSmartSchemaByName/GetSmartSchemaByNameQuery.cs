using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.SmartSchemas.GetSmartSchemaByName;

public sealed class GetSmartSchemaByNameQuery : IQuery<StsSmartSchemaDataModel>
{
    public GetSmartSchemaByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
