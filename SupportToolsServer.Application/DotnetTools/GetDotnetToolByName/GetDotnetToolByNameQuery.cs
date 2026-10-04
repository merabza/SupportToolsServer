using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.DotnetTools.GetDotnetToolByName;

public sealed class GetDotnetToolByNameQuery : IQuery<StsDotnetToolDataModel>
{
    public GetDotnetToolByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
