using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ProjectTemplates.DeleteProjectTemplate;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteProjectTemplateCommand : ICommand
{
    public DeleteProjectTemplateCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
