using ApiKeysManagement.Domain;
using SupportToolsServerDb.Models;

namespace SupportToolsServerMappers;

public static class ApiKeyMapper
{
    public static ApiKeyAndRemoteIpAddressDomain AdaptTo(this ApiKeyByRemoteIpAddress adaptFrom)
    {
        return new ApiKeyAndRemoteIpAddressDomain
        {
            ApiKey = adaptFrom.ApiKey, RemoteIpAddress = adaptFrom.RemoteIpAddress
        };
    }
}