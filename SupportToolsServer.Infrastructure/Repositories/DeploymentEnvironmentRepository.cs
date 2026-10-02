using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerCore.Application.Abstractions;
using SupportToolsServerCore.Domain.DeploymentEnvironments;

namespace SupportToolsServer.Infrastructure.Repositories;

public sealed class DeploymentEnvironmentRepository : IDeploymentEnvironmentRepository
{
    private readonly ISupportToolsServerDbContext _dbContext;

    public DeploymentEnvironmentRepository(ISupportToolsServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //ჩანაწერები თვალყურის დევნების გარეშე იკითხება, ცვლილებები კი Add/Update/Delete-ით რეგისტრირდება
    public Task<List<DeploymentEnvironment>> GetAll(CancellationToken cancellationToken)
    {
        return _dbContext.Environments.AsNoTracking().ToListAsync(cancellationToken);
    }

    //სახელი რეგისტრის გარეშე (OrdinalIgnoreCase) მეხსიერებაში ედრება, რომ შედეგი ბაზის collation-ზე არ იყოს
    //დამოკიდებული. ცხრილი პატარაა, ამიტომ ყველა ჩანაწერი იკითხება
    public async Task<DeploymentEnvironment?> GetByName(string name, CancellationToken cancellationToken)
    {
        List<DeploymentEnvironment> environments = await GetAll(cancellationToken);
        return environments.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    //წაკითხული ეგზემპლარის ვერსია concurrency token-ის ორიგინალია: DELETE ... WHERE Version = <წაკითხული>
    public void Delete(DeploymentEnvironment o)
    {
        _dbContext.Environments.Remove(o);
    }

    public void Add(DeploymentEnvironment crudEntity)
    {
        _dbContext.Environments.Add(crudEntity);
    }

    public void Update(DeploymentEnvironment crudEntity)
    {
        _dbContext.Environments.Update(crudEntity).ExpectPreviousVersion();
    }
}
