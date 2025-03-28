using System.Threading.Tasks;
using ApiKeysManagement;
using ApiKeysManagement.Domain;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerDb;
using SupportToolsServerMappers;

namespace SupportToolsServerRepositories;

public class ApiKeyByDatabaseFinder : IApiKeyFinder
{
    private readonly SupportToolsServerDbContext _dbContext;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApiKeyByDatabaseFinder(SupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiKeyAndRemoteIpAddressDomain?> GetApiKeyAndRemAddress(string apiKey, string remoteIpAddress)
    {
        return (await _dbContext.ApiKeysByRemoteIpAddresses.SingleOrDefaultAsync(x =>
            x.ApiKey == apiKey && x.RemoteIpAddress == remoteIpAddress))?.AdaptTo();
    }
}