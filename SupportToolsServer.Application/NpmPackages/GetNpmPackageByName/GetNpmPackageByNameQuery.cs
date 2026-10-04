using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.NpmPackages.GetNpmPackageByName;

public sealed class GetNpmPackageByNameQuery : IQuery<StsNpmPackageDataModel>
{
    public GetNpmPackageByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
