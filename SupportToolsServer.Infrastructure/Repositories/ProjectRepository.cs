using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.Projects;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public ProjectRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //პროექტები შვილებით და თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<Project>> GetAll(CancellationToken cancellationToken)
    {
        return WithChildren(_dbContext.Projects.AsNoTracking()).ToListAsync(cancellationToken);
    }

    //დიდი აგრეგატია, ამიტომ ჯერ Id და სახელი იკითხება და მხოლოდ ნაპოვნი პროექტი იტვირთება
    public async Task<Project?> GetByName(string name, CancellationToken cancellationToken)
    {
        ProjectId? id = await FindId(name, cancellationToken);
        if (id is null)
        {
            return null;
        }

        return await WithChildren(_dbContext.Projects.AsNoTracking())
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    //სახელი იგივე წესით ედრება, მაგრამ ნაპოვნი პროექტი თვალყურის დევნით იკითხება: EF-მა მისი შვილები უნდა იცოდეს, რომ
    //განახლებისას ჩანაცვლებული შვილები წაშალოს (CLAUDE.md, Registry conventions)
    public async Task<Project?> GetByNameForUpdate(string name, CancellationToken cancellationToken)
    {
        ProjectId? id = await FindId(name, cancellationToken);
        if (id is null)
        {
            return null;
        }

        return await WithChildren(_dbContext.Projects).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>. შვილები
    //პროექტთან ერთად იშლება
    public void Delete(Project o)
    {
        _dbContext.Projects.Remove(o);
    }

    public void Add(Project crudEntity)
    {
        _dbContext.Projects.Add(crudEntity);
    }

    //პროექტი GetByNameForUpdate-მა თვალყურის დევნით წაიკითხა, ამიტომ შენახვისას ჩანაცვლებული შვილები იშლება, ახლები
    //ემატება და ბაზის პარამეტრები იცვლება, იმავე ტრანზაქციაში, რომელიც პროექტს ვერსიის შემოწმებით ანახლებს
    public void Update(Project crudEntity)
    {
        _dbContext.Projects.Update(crudEntity).ExpectPreviousVersion();
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული
    private async Task<ProjectId?> FindId(string name, CancellationToken cancellationToken)
    {
        var names = await _dbContext.Projects.AsNoTracking().Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);
        return names.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    //შვილების ექვსი სია ცალკე მოთხოვნებით იკითხება (AsSplitQuery), რომ JOIN-ებმა სტრიქონები არ გაამრავლოს. ბაზის
    //პარამეტრები პროექტის სტრიქონშია და ავტომატურად იკითხება
    private static IQueryable<Project> WithChildren(IQueryable<Project> projects)
    {
        return projects.Include(x => x.GitRepos).Include(x => x.NpmPackages).Include(x => x.RedundantFiles)
            .Include(x => x.AllowedTools).Include(x => x.Endpoints).Include(x => x.RouteClasses).AsSplitQuery();
    }
}
