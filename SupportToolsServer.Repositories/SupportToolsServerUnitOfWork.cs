using RepositoriesShared;
using SupportToolsServer.Persistence;

namespace SupportToolsServer.Repositories;

public class SupportToolsServerUnitOfWork : UnitOfWork
{
    public SupportToolsServerUnitOfWork(SupportToolsServerDbContext dbContext) : base(dbContext)
    {
    }
}