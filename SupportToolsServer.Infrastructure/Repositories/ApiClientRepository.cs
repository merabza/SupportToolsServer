using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.ApiClients;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class ApiClientRepository : IApiClientRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public ApiClientRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<ApiClient>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.ApiClients.AsNoTracking().ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<ApiClient?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<ApiClient> apiClients = await GetAll(cancellationToken);
        return apiClients.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>.
    //ჩანაწერს, რომელსაც სხვა აგრეგატი მიმართავს, ბაზის FK (Restrict) არ შლის
    public void Delete(ApiClient o)
    {
        _dbContext.ApiClients.Remove(o);
    }

    public void Add(ApiClient crudEntity)
    {
        _dbContext.ApiClients.Add(crudEntity);
    }

    public void Update(ApiClient crudEntity)
    {
        _dbContext.ApiClients.Update(crudEntity).ExpectPreviousVersion();
    }
}
