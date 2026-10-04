using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.ReactAppTemplates;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class ReactAppTemplateRepository : IReactAppTemplateRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public ReactAppTemplateRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<ReactAppTemplate>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.ReactAppTemplates.AsNoTracking().ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<ReactAppTemplate?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<ReactAppTemplate> reactAppTemplates = await GetAll(cancellationToken);
        return reactAppTemplates.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>
    public void Delete(ReactAppTemplate o)
    {
        _dbContext.ReactAppTemplates.Remove(o);
    }

    public void Add(ReactAppTemplate crudEntity)
    {
        _dbContext.ReactAppTemplates.Add(crudEntity);
    }

    public void Update(ReactAppTemplate crudEntity)
    {
        _dbContext.ReactAppTemplates.Update(crudEntity).ExpectPreviousVersion();
    }
}
