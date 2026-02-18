using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.SqlServerDbTools;

namespace SupportToolsServerCommandRepositories;

public class SqlDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken token = default)
    {
        var sqlKit = new SqlKit();
        // ReSharper disable once using
        DbConnection connection = sqlKit.GetConnection();
        connection.ConnectionString = _connectionString;
        await connection.OpenAsync(token);
        return connection;
    }
}
