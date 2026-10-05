using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.ProjectTemplates;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class ProjectTemplateRepository : IProjectTemplateRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public ProjectTemplateRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<ProjectTemplate>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.ProjectTemplates.AsNoTracking().ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<ProjectTemplate?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<ProjectTemplate> projectTemplates = await GetAll(cancellationToken);
        return projectTemplates.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>
    public void Delete(ProjectTemplate o)
    {
        _dbContext.ProjectTemplates.Remove(o);
    }

    public void Add(ProjectTemplate crudEntity)
    {
        _dbContext.ProjectTemplates.Add(crudEntity);
    }

    public void Update(ProjectTemplate crudEntity)
    {
        _dbContext.ProjectTemplates.Update(crudEntity).ExpectPreviousVersion();
    }
}
