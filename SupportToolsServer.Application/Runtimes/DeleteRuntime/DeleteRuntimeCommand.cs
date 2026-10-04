using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Runtimes.DeleteRuntime;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteRuntimeCommand : ICommand
{
    public DeleteRuntimeCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
