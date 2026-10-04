using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.Servers;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class ServerRepository : IServerRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public ServerRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<Server>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.Servers.AsNoTracking().ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<Server?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<Server> servers = await GetAll(cancellationToken);
        return servers.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>
    public void Delete(Server o)
    {
        _dbContext.Servers.Remove(o);
    }

    public void Add(Server crudEntity)
    {
        _dbContext.Servers.Add(crudEntity);
    }

    public void Update(Server crudEntity)
    {
        _dbContext.Servers.Update(crudEntity).ExpectPreviousVersion();
    }
}
