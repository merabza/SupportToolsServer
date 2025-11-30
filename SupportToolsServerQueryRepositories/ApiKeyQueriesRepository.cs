using System.Threading.Tasks;
using ApiKeysManagement.Domain;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerApiKeyIdentity;
using SupportToolsServerDb;
using SupportToolsServerMappers;

namespace SupportToolsServerQueryRepositories;

public class ApiKeyQueriesRepository : IApiKeyQueriesRepository
{
    private readonly SupportToolsServerDbContext _dbContext;

    public ApiKeyQueriesRepository(SupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiKeyAndRemoteIpAddressDomain?> GetApiKeyAndRemAddress(string apiKey, string remoteIpAddress)
    {
        return (await _dbContext.ApiKeysByRemoteIpAddresses.SingleOrDefaultAsync(x =>
            x.ApiKey == apiKey && x.RemoteIpAddress == remoteIpAddress))?.AdaptTo();
    }
}