using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteDatabaseServerConnectionCommand : ICommand
{
    public DeleteDatabaseServerConnectionCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
