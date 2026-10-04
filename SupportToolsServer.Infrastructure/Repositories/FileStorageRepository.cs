using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.FileStorages;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class FileStorageRepository : IFileStorageRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public FileStorageRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<FileStorage>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.FileStorages.AsNoTracking().ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<FileStorage?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<FileStorage> fileStorages = await GetAll(cancellationToken);
        return fileStorages.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>
    public void Delete(FileStorage o)
    {
        _dbContext.FileStorages.Remove(o);
    }

    public void Add(FileStorage crudEntity)
    {
        _dbContext.FileStorages.Add(crudEntity);
    }

    public void Update(FileStorage crudEntity)
    {
        _dbContext.FileStorages.Update(crudEntity).ExpectPreviousVersion();
    }
}
