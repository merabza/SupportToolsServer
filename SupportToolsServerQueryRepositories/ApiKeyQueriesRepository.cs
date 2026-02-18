using System;
using System.Threading.Tasks;
using SupportToolsServer.Persistence;
using SupportToolsServerApiKeyIdentity;
using SystemTools.ApiKeysManagement.Domain;

namespace SupportToolsServerQueryRepositories;

public class ApiKeyQueriesRepository : IApiKeyQueriesRepository
{
    //private readonly SupportToolsServerDbContext _dbContext;

    public ApiKeyQueriesRepository(SupportToolsServerDbContext dbContext)
    {
        //_dbContext = dbContext;
    }

    public Task<ApiKeyAndRemoteIpAddressDomain?> GetApiKeyAndRemAddress(string apiKey, string remoteIpAddress)
    {
        //return (await _dbContext.ApiKeysByRemoteIpAddresses.SingleOrDefaultAsync(x =>
        //    x.ApiKey == apiKey && x.RemoteIpAddress == remoteIpAddress))?.AdaptTo();
        throw new NotImplementedException();
    }
}
