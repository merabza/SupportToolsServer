using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.DatabaseServerConnections.UpdateDatabaseServerConnection;

//upsert ვერსიით (CLAUDE.md, Registry conventions). DatabaseServerConnection.Version მოსალოდნელი ვერსიაა,
//DatabaseServerConnection.Name-ს კი ენდპოინტი მისამართის key-ით ავსებს. განახლება კავშირს folders set-ებიანად
//ანაცვლებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateDatabaseServerConnectionCommand : ICommand<int>
{
    public UpdateDatabaseServerConnectionCommand(StsDatabaseServerConnectionDataModel databaseServerConnection)
    {
        DatabaseServerConnection = databaseServerConnection;
    }

    public StsDatabaseServerConnectionDataModel DatabaseServerConnection { get; }
}
