using System;
using System.Collections.Generic;
using System.Linq;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;

namespace SupportToolsServer.Application.DatabaseServerConnections;

internal static class DatabaseServerConnectionContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "DatabaseServerConnection";

    //ვებაგენტი ბაზაში ApiClient-ის Id-ით ინახება, კონტრაქტში კი მისი სახელით გადაიცემა. folders set-ები სახელით
    //ლაგდება: კლიენტის ჰეში რიგზე არ უნდა იყოს დამოკიდებული
    public static StsDatabaseServerConnectionDataModel ToContractModel(this DatabaseServerConnection connection,
        IReadOnlyDictionary<ApiClientId, string> apiClientNames)
    {
        return new StsDatabaseServerConnectionDataModel
        {
            Name = connection.Name,
            DatabaseServerProvider = connection.DatabaseServerProvider,
            DbWebAgentName = connection.DbWebAgentId is null ? null : apiClientNames[connection.DbWebAgentId],
            RemoteDbConnectionName = connection.RemoteDbConnectionName,
            ServerAddress = connection.ServerAddress,
            WindowsNtIntegratedSecurity = connection.WindowsNtIntegratedSecurity,
            ServerUser = connection.ServerUser,
            ServerPass = connection.ServerPass,
            TrustServerCertificate = connection.TrustServerCertificate,
            ConnectionTimeOut = connection.ConnectionTimeOut,
            Encrypt = connection.Encrypt,
            DatabaseFoldersSets =
            [
                .. connection.DatabaseFoldersSets.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x =>
                    new StsDatabaseFoldersSetDataModel
                    {
                        Name = x.Name, Backup = x.Backup, Data = x.Data, DataLog = x.DataLog
                    })
            ],
            Version = connection.Version
        };
    }

    //სხვა აგრეგატები ბაზის კავშირს Id-ით ინახავენ, კონტრაქტში კი მის სახელს გადასცემენ
    //(ProjectCreatorSettings.DeveloperDbConnectionName)
    public static Dictionary<DatabaseServerConnectionId, string> ToNamesById(
        this IEnumerable<DatabaseServerConnection> connections)
    {
        return connections.ToDictionary(x => x.Id, x => x.Name);
    }
}
