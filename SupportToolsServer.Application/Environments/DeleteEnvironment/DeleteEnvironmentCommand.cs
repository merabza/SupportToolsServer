using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Environments.DeleteEnvironment;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteEnvironmentCommand : ICommand
{
    public DeleteEnvironmentCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
