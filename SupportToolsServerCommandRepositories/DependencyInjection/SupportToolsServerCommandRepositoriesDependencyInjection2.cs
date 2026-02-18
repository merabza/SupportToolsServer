using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace SupportToolsServerCommandRepositories.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerCommandRepositoriesDependencyInjection2
{
    public static IServiceCollection AddSupportToolsServerForCommandsDatabase(this IServiceCollection services,
        ILogger? debugLogger, IConfiguration configuration)
    {
        debugLogger?.Information("{MethodName} Started", nameof(AddSupportToolsServerForCommandsDatabase));

        const string connectionStringConfigurationKey = "Data:SupportToolsServerDatabase:ConnectionString";
        string? connectionString = configuration[connectionStringConfigurationKey];

        if (string.IsNullOrWhiteSpace(connectionString) && debugLogger is not null)
        {
            debugLogger.Error("Parameter {ConnectionStringConfigurationKey} is empty",
                connectionStringConfigurationKey);
            return services;
        }

        services.AddSingleton<IDbConnectionFactory>(_ =>
            new SqlDbConnectionFactory(configuration[connectionStringConfigurationKey]!));

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerForCommandsDatabase));

        return services;
    }
}
