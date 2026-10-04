using System.Collections.Generic;
using System.Linq;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;

namespace SupportToolsServer.Application.ApiClients;

internal static class ApiClientContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse,
    //ReferencedRecordsNotFound)
    public const string EntityName = "ApiClient";

    public static StsApiClientDataModel ToContractModel(this ApiClient apiClient)
    {
        return new StsApiClientDataModel
        {
            Name = apiClient.Name, Server = apiClient.Server, ApiKey = apiClient.ApiKey, Version = apiClient.Version
        };
    }

    //სხვა აგრეგატები ApiClient-ს Id-ით ინახავენ, კონტრაქტში კი მის სახელს გადასცემენ (მაგალითად, DbWebAgentName)
    public static Dictionary<ApiClientId, string> ToNamesById(this IEnumerable<ApiClient> apiClients)
    {
        return apiClients.ToDictionary(x => x.Id, x => x.Name);
    }
}
