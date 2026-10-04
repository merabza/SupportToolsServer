using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplateByName;

public sealed class GetReactAppTemplateByNameQuery : IQuery<StsReactAppTemplateDataModel>
{
    public GetReactAppTemplateByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
