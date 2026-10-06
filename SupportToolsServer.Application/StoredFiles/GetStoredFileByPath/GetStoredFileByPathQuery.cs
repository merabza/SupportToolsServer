using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.StoredFiles.GetStoredFileByPath;

public sealed class GetStoredFileByPathQuery : IQuery<StsStoredFileDataModel>
{
    public GetStoredFileByPathQuery(string path)
    {
        Path = path;
    }

    public string Path { get; }
}
