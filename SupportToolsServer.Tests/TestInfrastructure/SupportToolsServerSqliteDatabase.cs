using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerDbPart.Db;

namespace SupportToolsServer.Tests.TestInfrastructure;

//An in-memory SQLite database created from the real SupportToolsServerDbContext model (unique indexes, foreign key).
//The connection stays open for the lifetime of the object, so every context from NewContext() sees the same data;
//one context per operation mirrors one context per host request
internal sealed class SupportToolsServerSqliteDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SupportToolsServerDbContext> _options;

    private SupportToolsServerSqliteDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        _options = new DbContextOptionsBuilder<SupportToolsServerDbContext>().UseSqlite(_connection).Options;
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    public static async Task<SupportToolsServerSqliteDatabase> CreateAsync()
    {
        var database = new SupportToolsServerSqliteDatabase();
        await database._connection.OpenAsync();

        await using SupportToolsServerDbContext context = database.NewContext();
        await context.Database.EnsureCreatedAsync();
        return database;
    }

    public SupportToolsServerDbContext NewContext()
    {
        return new SupportToolsServerDbContext(_options);
    }
}
