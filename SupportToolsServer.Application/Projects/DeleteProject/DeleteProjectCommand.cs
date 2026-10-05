using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Projects.DeleteProject;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteProjectCommand : ICommand
{
    public DeleteProjectCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
