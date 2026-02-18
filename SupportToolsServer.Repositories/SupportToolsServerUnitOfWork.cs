using SupportToolsServer.Persistence;
using SystemTools.RepositoriesShared;

namespace SupportToolsServer.Repositories;

public class SupportToolsServerUnitOfWork : UnitOfWork
{
    public SupportToolsServerUnitOfWork(SupportToolsServerDbContext dbContext) : base(dbContext)
    {
    }
}
