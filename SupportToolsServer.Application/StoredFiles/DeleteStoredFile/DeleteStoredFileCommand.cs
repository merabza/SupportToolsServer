using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.StoredFiles.DeleteStoredFile;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteStoredFileCommand : ICommand
{
    public DeleteStoredFileCommand(string path, int? version)
    {
        Path = path;
        Version = version;
    }

    public string Path { get; }

    public int? Version { get; }
}
