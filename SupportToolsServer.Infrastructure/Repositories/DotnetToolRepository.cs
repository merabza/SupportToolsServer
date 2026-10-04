using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.DotnetTools;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class DotnetToolRepository : IDotnetToolRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public DotnetToolRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<DotnetTool>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.DotnetTools.AsNoTracking().ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<DotnetTool?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<DotnetTool> dotnetTools = await GetAll(cancellationToken);
        return dotnetTools.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>
    public void Delete(DotnetTool o)
    {
        _dbContext.DotnetTools.Remove(o);
    }

    public void Add(DotnetTool crudEntity)
    {
        _dbContext.DotnetTools.Add(crudEntity);
    }

    public void Update(DotnetTool crudEntity)
    {
        _dbContext.DotnetTools.Update(crudEntity).ExpectPreviousVersion();
    }
}
