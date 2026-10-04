using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.FileStorages.DeleteFileStorage;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteFileStorageCommand : ICommand
{
    public DeleteFileStorageCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
