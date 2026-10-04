using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteSmartSchemaCommand : ICommand
{
    public DeleteSmartSchemaCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
