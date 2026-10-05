using System.Collections.Generic;
using System.Linq;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;

namespace SupportToolsServer.Application.Servers;

internal static class ServerContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "Server";

    //ვებაგენტები და Runtime ბაზაში Id-ებით ინახება, კონტრაქტში კი მათი სახელებით გადაიცემა
    public static StsServerDataModel ToContractModel(this Server server,
        IReadOnlyDictionary<ApiClientId, string> apiClientNames, IReadOnlyDictionary<RuntimeId, string> runtimeNames)
    {
        return new StsServerDataModel
        {
            Name = server.Name,
            WebAgentName = server.WebAgentId is null ? null : apiClientNames[server.WebAgentId],
            WebAgentInstallerName =
                server.WebAgentInstallerId is null ? null : apiClientNames[server.WebAgentInstallerId],
            FilesUserName = server.FilesUserName,
            FilesUsersGroupName = server.FilesUsersGroupName,
            Runtime = server.RuntimeId is null ? null : runtimeNames[server.RuntimeId],
            ServerSideDownloadFolder = server.ServerSideDownloadFolder,
            ServerSideDeployFolder = server.ServerSideDeployFolder,
            Version = server.Version
        };
    }

    //სხვა აგრეგატები სერვერს Id-ით ინახავენ, კონტრაქტში კი მის სახელს გადასცემენ
    //(ProjectCreatorSettings.ProductionServerName)
    public static Dictionary<ServerId, string> ToNamesById(this IEnumerable<Server> servers)
    {
        return servers.ToDictionary(x => x.Id, x => x.Name);
    }
}
