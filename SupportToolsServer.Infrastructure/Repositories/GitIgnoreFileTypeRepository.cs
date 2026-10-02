using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;

namespace SupportToolsServer.Infrastructure.Repositories;

public class GitIgnoreFileTypeRepository : IGitIgnoreFileTypeRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public GitIgnoreFileTypeRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<GitIgnoreFileType>> GetAll(CancellationToken cancellationToken)
    {
        //ჩანაწერები მხოლოდ შესადარებლად იტვირთება. თვალყურის დევნებისას Update-ისთვის გადაცემული
        //იგივე Id-ის მქონე ახალი ობიექტი EF-ში შეცდომას გამოიწვევდა
        return _dbContext.GitIgnoreFileTypes.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<GitIgnoreFileType?> GetByName(string name, CancellationToken cancellationToken)
    {
        return _dbContext.GitIgnoreFileTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name,
            cancellationToken);
    }

    public void Delete(GitIgnoreFileType o)
    {
        _dbContext.GitIgnoreFileTypes.Remove(o);
    }

    public void Add(GitIgnoreFileType crudEntity)
    {
        _dbContext.GitIgnoreFileTypes.Add(crudEntity);
    }

    //ახალი ეგზემპლარი შენახული Version + 1-ით მოდის, ამიტომ concurrency token-ის ორიგინალი წინა ვერსიაა
    public void Update(GitIgnoreFileType crudEntity)
    {
        _dbContext.GitIgnoreFileTypes.Update(crudEntity).ExpectPreviousVersion();
    }
}
