using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Projects.GetProjectByName;

public sealed class GetProjectByNameQuery : IQuery<StsProjectDataModel>
{
    public GetProjectByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
