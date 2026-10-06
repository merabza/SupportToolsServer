using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.StoredFiles;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class StoredFileRepository : IStoredFileRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public StoredFileRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //მეტამონაცემების პროექცია: SELECT შიგთავსის (nvarchar(max)) სვეტს არ კითხულობს
    public Task<List<StoredFileInfo>> GetInfos(CancellationToken cancellationToken)
    {
        return _dbContext.StoredFiles.AsNoTracking()
            .Select(x => new StoredFileInfo(x.Path, x.Sha256, x.Length, x.UpdatedAtUtc, x.Version))
            .ToListAsync(cancellationToken);
    }

    //ჯერ Id და გზა იკითხება და მხოლოდ ნაპოვნი ფაილი იტვირთება შიგთავსით. გზა რეგისტრის გარეშე (OrdinalIgnoreCase)
    //მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს დამოკიდებული. ჩანაწერები თვალყურის დევნების გარეშე
    //იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public async Task<StoredFile?> GetByPath(string path, CancellationToken cancellationToken)
    {
        var paths = await _dbContext.StoredFiles.AsNoTracking().Select(x => new { x.Id, x.Path })
            .ToListAsync(cancellationToken);
        StoredFileId? id = paths
            .SingleOrDefault(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))?.Id;
        if (id is null)
        {
            return null;
        }

        return await _dbContext.StoredFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public void Add(StoredFile storedFile)
    {
        _dbContext.StoredFiles.Add(storedFile);
    }

    public void Update(StoredFile storedFile)
    {
        _dbContext.StoredFiles.Update(storedFile).ExpectPreviousVersion();
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>
    public void Delete(StoredFile storedFile)
    {
        _dbContext.StoredFiles.Remove(storedFile);
    }
}
