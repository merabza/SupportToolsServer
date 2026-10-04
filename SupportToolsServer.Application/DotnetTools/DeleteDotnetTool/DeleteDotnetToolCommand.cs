using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.DotnetTools.DeleteDotnetTool;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteDotnetToolCommand : ICommand
{
    public DeleteDotnetToolCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
