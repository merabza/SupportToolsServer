using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.Runtimes;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class RuntimeRepository : IRuntimeRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public RuntimeRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<Runtime>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.Runtimes.AsNoTracking().ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<Runtime?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<Runtime> runtimes = await GetAll(cancellationToken);
        return runtimes.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>
    public void Delete(Runtime o)
    {
        _dbContext.Runtimes.Remove(o);
    }

    public void Add(Runtime crudEntity)
    {
        _dbContext.Runtimes.Add(crudEntity);
    }

    public void Update(Runtime crudEntity)
    {
        _dbContext.Runtimes.Update(crudEntity).ExpectPreviousVersion();
    }
}
