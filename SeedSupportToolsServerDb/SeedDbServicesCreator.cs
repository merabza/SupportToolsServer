using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupportToolsNewDataSeeding;
using SupportToolsServerDb;
using SupportToolsServerDbDataSeeding;
using SupportToolsServerDbNewDataSeeding;
using SystemToolsShared;

namespace SeedSupportToolsServerDb;

public sealed class SeedDbServicesCreator : ServicesCreator
{
    private readonly string _connectionString;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SeedDbServicesCreator(string? logFolder, string? logFileName, string appName, string connectionString) :
        base(logFolder, logFileName, appName)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddScoped<IStsDataSeederRepository, StsDataSeederRepository>();

        services.AddScoped<IDataFixRepository, DataFixRepository>();

        services.AddDbContext<SupportToolsServerDbContext>(options => options.UseSqlServer(_connectionString));
    }
}