using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Servers.UpdateServer;

//upsert ვერსიით (CLAUDE.md, Registry conventions). Server.Version მოსალოდნელი ვერსიაა, Server.Name-ს კი ენდპოინტი
//მისამართის key-ით ავსებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateServerCommand : ICommand<int>
{
    public UpdateServerCommand(StsServerDataModel server)
    {
        Server = server;
    }

    public StsServerDataModel Server { get; }
}
