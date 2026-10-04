using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.NpmPackages.DeleteNpmPackage;

//Version მოსალოდნელი ვერსიაა. null ნიშნავს უპირობო წაშლას, ხელით რედაქტორებისთვის
public sealed class DeleteNpmPackageCommand : ICommand
{
    public DeleteNpmPackageCommand(string name, int? version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; }

    public int? Version { get; }
}
