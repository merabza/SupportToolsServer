using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.NpmPackages;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class NpmPackageRepository : INpmPackageRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public NpmPackageRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<NpmPackage>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.NpmPackages.AsNoTracking().ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<NpmPackage?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<NpmPackage> npmPackages = await GetAll(cancellationToken);
        return npmPackages.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>
    public void Delete(NpmPackage o)
    {
        _dbContext.NpmPackages.Remove(o);
    }

    public void Add(NpmPackage crudEntity)
    {
        _dbContext.NpmPackages.Add(crudEntity);
    }

    public void Update(NpmPackage crudEntity)
    {
        _dbContext.NpmPackages.Update(crudEntity).ExpectPreviousVersion();
    }
}
