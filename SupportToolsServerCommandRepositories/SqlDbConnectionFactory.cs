using System.Data;
using System.Threading;
using System.Threading.Tasks;
using SqlServerDbTools;

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
        var connection = sqlKit.GetConnection();
        connection.ConnectionString = _connectionString;
        await connection.OpenAsync(token);
        return connection;
    }
}