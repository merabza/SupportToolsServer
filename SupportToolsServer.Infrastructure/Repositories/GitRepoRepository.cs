using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.GitRepos;

namespace SupportToolsServer.Infrastructure.Repositories;

public class GitRepoRepository : IGitRepoRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public GitRepoRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება,
    //ისევე როგორც GitIgnoreFileTypeRepository-ში
    public Task<List<GitRepo>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.GitRepos.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<GitRepo?> GetByName(string name, CancellationToken cancellationToken)
    {
        return _dbContext.GitRepos.AsNoTracking().SingleOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public void Delete(GitRepo o)
    {
        _dbContext.GitRepos.Remove(o);
    }

    public void Add(GitRepo crudEntity)
    {
        _dbContext.GitRepos.Add(crudEntity);
    }

    //GitRepo.Update ვერსიას ზრდის, ამიტომ concurrency token-ის ორიგინალი წინა ვერსიაა
    public void Update(GitRepo crudEntity)
    {
        _dbContext.GitRepos.Update(crudEntity).ExpectPreviousVersion();
    }
}
