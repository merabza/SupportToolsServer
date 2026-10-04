using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Servers.DeleteServer;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteServerCommand : ICommand
{
    public DeleteServerCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
