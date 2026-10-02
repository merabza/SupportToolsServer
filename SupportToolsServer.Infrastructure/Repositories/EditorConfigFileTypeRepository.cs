using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;

namespace SupportToolsServer.Infrastructure.Repositories;

public class EditorConfigFileTypeRepository : IEditorConfigFileTypeRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public EditorConfigFileTypeRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<EditorConfigFileType>> GetAll(CancellationToken cancellationToken)
    {
        //ჩანაწერები მხოლოდ შესადარებლად იტვირთება. თვალყურის დევნებისას Update-ისთვის გადაცემული
        //იგივე Id-ის მქონე ახალი ობიექტი EF-ში შეცდომას გამოიწვევდა
        return _dbContext.EditorConfigFileTypes.AsNoTracking().ToListAsync(cancellationToken);
    }

    public void Delete(EditorConfigFileType o)
    {
        _dbContext.EditorConfigFileTypes.Remove(o);
    }

    public void Add(EditorConfigFileType crudEntity)
    {
        _dbContext.EditorConfigFileTypes.Add(crudEntity);
    }

    //ახალი ეგზემპლარი შენახული Version + 1-ით მოდის, ამიტომ concurrency token-ის ორიგინალი წინა ვერსიაა
    public void Update(EditorConfigFileType crudEntity)
    {
        _dbContext.EditorConfigFileTypes.Update(crudEntity).ExpectPreviousVersion();
    }
}
