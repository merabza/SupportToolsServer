using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.FileStorages.GetFileStorageByName;

public sealed class GetFileStorageByNameQuery : IQuery<StsFileStorageDataModel>
{
    public GetFileStorageByNameQuery(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
