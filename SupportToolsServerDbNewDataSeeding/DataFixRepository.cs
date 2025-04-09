using Microsoft.Extensions.Logging;
using SupportToolsNewDataSeeding;
using SupportToolsServerDb;

namespace SupportToolsServerDbNewDataSeeding;

public sealed class DataFixRepository : IDataFixRepository
{
    private readonly SupportToolsServerDbContext _context;
    private readonly ILogger<DataFixRepository> _logger;

    public DataFixRepository(SupportToolsServerDbContext context, ILogger<DataFixRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
}