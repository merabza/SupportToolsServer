using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Application.Data;
using SupportToolsServer.Domain.GitIgnoreFileTypes;

namespace SupportToolsServer.Repositories;

public class GitIgnoreFileTypeRepository : IGitIgnoreFileTypeRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public GitIgnoreFileTypeRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<GitIgnoreFileType>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.GitIgnoreFileTypes.ToListAsync(cancellationToken);
    }

    public void Delete(GitIgnoreFileType o)
    {
        _dbContext.GitIgnoreFileTypes.Remove(o);
    }

    public void Add(GitIgnoreFileType crudEntity)
    {
        _dbContext.GitIgnoreFileTypes.Add(crudEntity);
    }

    public void Update(GitIgnoreFileType crudEntity)
    {
        _dbContext.GitIgnoreFileTypes.Update(crudEntity);
    }
}