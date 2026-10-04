using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.DatabaseServerConnections;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class DatabaseServerConnectionRepository : IDatabaseServerConnectionRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public DatabaseServerConnectionRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //კავშირები folders set-ებით და თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<DatabaseServerConnection>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.DatabaseServerConnections.AsNoTracking().Include(x => x.DatabaseFoldersSets)
            .ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<DatabaseServerConnection?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<DatabaseServerConnection> connections = await GetAll(cancellationToken);
        return connections.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //სახელი იგივე წესით ედრება, მაგრამ თვალყურის დევნით იკითხება მხოლოდ ნაპოვნი კავშირი: EF-მა მისი folders set-ები
    //უნდა იცოდეს, რომ განახლებისას ჩანაცვლებული ნაკრებები წაშალოს (CLAUDE.md, Registry conventions)
    public async Task<DatabaseServerConnection?> GetByNameForUpdate(string name, CancellationToken cancellationToken)
    {
        var names = await _dbContext.DatabaseServerConnections.AsNoTracking().Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);
        DatabaseServerConnectionId? id = names
            .SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?.Id;
        if (id is null)
        {
            return null;
        }

        return await _dbContext.DatabaseServerConnections.Include(x => x.DatabaseFoldersSets)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>.
    //folders set-ები კავშირთან ერთად იშლება
    public void Delete(DatabaseServerConnection o)
    {
        _dbContext.DatabaseServerConnections.Remove(o);
    }

    public void Add(DatabaseServerConnection crudEntity)
    {
        _dbContext.DatabaseServerConnections.Add(crudEntity);
    }

    //კავშირი GetByNameForUpdate-მა თვალყურის დევნით წაიკითხა, ამიტომ შენახვისას ჩანაცვლებული folders set-ები იშლება,
    //ახლები კი ემატება, იმავე ტრანზაქციაში, რომელიც კავშირს ვერსიის შემოწმებით ანახლებს
    public void Update(DatabaseServerConnection crudEntity)
    {
        _dbContext.DatabaseServerConnections.Update(crudEntity).ExpectPreviousVersion();
    }
}
