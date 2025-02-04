//Created by RepositoryInterfaceCreator at 2/4/2025 7:31:10 PM

using Microsoft.EntityFrameworkCore.Storage;

namespace LibSupportToolsServerRepositories;

public interface ISupportToolsServerRepository
{
    int SaveChanges();
    int SaveChangesWithTransaction();
    IDbContextTransaction GetTransaction();
}