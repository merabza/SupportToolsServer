using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class SmartSchemaRepository : ISmartSchemaRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public SmartSchemaRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //სქემები დეტალებით და თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<SmartSchema>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.SmartSchemas.AsNoTracking().Include(x => x.Details).ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<SmartSchema?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<SmartSchema> smartSchemas = await GetAll(cancellationToken);
        return smartSchemas.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //სახელი იგივე წესით ედრება, მაგრამ თვალყურის დევნით იკითხება მხოლოდ ნაპოვნი სქემა: EF-მა მისი დეტალები უნდა იცოდეს,
    //რომ განახლებისას ჩანაცვლებული დეტალები წაშალოს (CLAUDE.md, Registry conventions)
    public async Task<SmartSchema?> GetByNameForUpdate(string name, CancellationToken cancellationToken)
    {
        var names = await _dbContext.SmartSchemas.AsNoTracking().Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);
        SmartSchemaId? id = names.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
            ?.Id;
        if (id is null)
        {
            return null;
        }

        return await _dbContext.SmartSchemas.Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>.
    //დეტალები სქემასთან ერთად იშლება
    public void Delete(SmartSchema o)
    {
        _dbContext.SmartSchemas.Remove(o);
    }

    public void Add(SmartSchema crudEntity)
    {
        _dbContext.SmartSchemas.Add(crudEntity);
    }

    //სქემა GetByNameForUpdate-მა თვალყურის დევნით წაიკითხა, ამიტომ შენახვისას ჩანაცვლებული დეტალები იშლება, ახლები კი
    //ემატება, იმავე ტრანზაქციაში, რომელიც სქემას ვერსიის შემოწმებით ანახლებს
    public void Update(SmartSchema crudEntity)
    {
        _dbContext.SmartSchemas.Update(crudEntity).ExpectPreviousVersion();
    }
}
