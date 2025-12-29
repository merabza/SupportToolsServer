using System.Threading;
using System.Threading.Tasks;

namespace SupportToolsServer.Application.Data;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}