using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ApiClients.DeleteApiClient;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteApiClientCommand : ICommand
{
    public DeleteApiClientCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
