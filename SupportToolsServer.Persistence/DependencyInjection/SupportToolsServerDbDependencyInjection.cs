using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace SupportToolsServer.Persistence.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerDbDependencyInjection
{
    public static IServiceCollection AddSupportToolsServerDb(this IServiceCollection services, ILogger? debugLogger,
        IConfiguration configuration)
    {
        debugLogger?.Information("{MethodName} Started", nameof(AddSupportToolsServerDb));

        const string connectionStringConfigKey = "Data:SupportToolsServerDatabase:ConnectionString";
        var connectionString = configuration[connectionStringConfigKey];

        if (string.IsNullOrWhiteSpace(connectionString) && debugLogger is not null)
        {
            debugLogger.Error("Parameter {ConnectionStringConfigKey} is empty", connectionStringConfigKey);
            return services;
        }

        services.AddDbContext<SupportToolsServerDbContext>(options => options.UseSqlServer(connectionString));
        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerDb));

        return services;
    }
}
