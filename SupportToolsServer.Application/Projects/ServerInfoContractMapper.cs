using System;
using System.Collections.Generic;
using System.Linq;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Projects;

namespace SupportToolsServer.Application.Projects;

//ServerInfo-ები, პროექტის აგრეგატის შვილები: სერვერი, გარემო, ვებაგენტი და ბაზის პარამეტრების მითითებები ბაზაში
//Id-ებით ინახება, კონტრაქტში კი სახელებით გადაიცემა
internal static class ServerInfoContractMapper
{
    //ServerInfo-ს გასაღები შეტყობინებებსა და მომხმარებლების სიაში, კლიენტის ServerInfoModel.GetItemKey-ის ფორმით
    public static string Key(string serverName, string environmentName)
    {
        return $"{serverName}|{environmentName}";
    }

    //სია ServerName-ითა და EnvironmentName-ით ლაგდება, ინსტრუმენტები კი სახელით: კლიენტის ჰეში რიგზე არ უნდა იყოს
    //დამოკიდებული
    public static List<StsServerInfoDataModel> ToContractModels(this IEnumerable<ServerInfo> serverInfos,
        ProjectReferenceNames names)
    {
        return
        [
            .. serverInfos.Select(x => x.ToContractModel(names))
                .OrderBy(x => x.ServerName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.EnvironmentName, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static StsServerInfoDataModel ToContractModel(this ServerInfo serverInfo, ProjectReferenceNames names)
    {
        return new StsServerInfoDataModel
        {
            ServerName = names.Servers[serverInfo.ServerId],
            EnvironmentName = names.Environments[serverInfo.EnvironmentId],
            WebAgentNameForCheck = names.ApiClients.GetName(serverInfo.WebAgentForCheckId),
            ServerSidePort = serverInfo.ServerSidePort,
            ApiVersionId = serverInfo.ApiVersionId,
            AppSettingsJsonSourceFileName = serverInfo.AppSettingsJsonSourceFileName,
            AppSettingsEncodedJsonFileName = serverInfo.AppSettingsEncodedJsonFileName,
            ServiceUserName = serverInfo.ServiceUserName,
            AllowToolsList =
                [.. serverInfo.AllowedTools.Select(x => x.ToolName).Order(StringComparer.OrdinalIgnoreCase)],
            CurrentDatabaseParameters = serverInfo.CurrentDatabaseParameters?.ToContractModel(
                names.DatabaseServerConnections, names.SmartSchemas, names.FileStorages),
            NewDatabaseParameters = serverInfo.NewDatabaseParameters?.ToContractModel(names.DatabaseServerConnections,
                names.SmartSchemas, names.FileStorages)
        };
    }
}
