using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace SupportToolsServerCommandRepositories;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken token = default);
}