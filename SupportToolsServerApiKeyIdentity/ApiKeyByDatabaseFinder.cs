using System.Threading.Tasks;
using SystemTools.ApiKeysManagement;
using SystemTools.ApiKeysManagement.Domain;

namespace SupportToolsServerApiKeyIdentity;

public sealed class ApiKeyByDatabaseFinder : IApiKeyFinder
{
    private readonly IApiKeyQueriesRepository _apiKeyQueriesRepository;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApiKeyByDatabaseFinder(IApiKeyQueriesRepository apiKeyQueriesRepository)
    {
        _apiKeyQueriesRepository = apiKeyQueriesRepository;
    }

    public Task<ApiKeyAndRemoteIpAddressDomain?> GetApiKeyAndRemAddress(string apiKey, string remoteIpAddress)
    {
        return _apiKeyQueriesRepository.GetApiKeyAndRemAddress(apiKey, remoteIpAddress);
    }
}
