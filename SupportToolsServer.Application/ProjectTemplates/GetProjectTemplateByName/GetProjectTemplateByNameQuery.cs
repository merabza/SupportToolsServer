using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ProjectTemplates.GetProjectTemplateByName;

public sealed class GetProjectTemplateByNameQuery : IQuery<StsProjectTemplateDataModel>
{
    public GetProjectTemplateByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
