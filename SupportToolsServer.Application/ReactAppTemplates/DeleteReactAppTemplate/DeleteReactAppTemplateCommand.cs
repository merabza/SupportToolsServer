using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.ReactAppTemplates.DeleteReactAppTemplate;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteReactAppTemplateCommand : ICommand
{
    public DeleteReactAppTemplateCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
