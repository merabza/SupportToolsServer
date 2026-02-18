using System.Threading.Tasks;
using SystemTools.ApiKeysManagement.Domain;

namespace SupportToolsServerApiKeyIdentity;

public interface IApiKeyQueriesRepository
{
    Task<ApiKeyAndRemoteIpAddressDomain?> GetApiKeyAndRemAddress(string apiKey, string remoteIpAddress);
}
