using System.Threading.Tasks;
using ApiKeysManagement.Domain;

namespace SupportToolsServerApiKeyIdentity;

public interface IApiKeyQueriesRepository
{
    Task<ApiKeyAndRemoteIpAddressDomain?> GetApiKeyAndRemAddress(string apiKey, string remoteIpAddress);
}