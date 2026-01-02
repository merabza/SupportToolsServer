using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SupportToolsServerCommandRepositories.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerCommandRepositoriesDependencyInjection2
{
    public static IServiceCollection AddSupportToolsServerForCommandsDatabase(this IServiceCollection services,
        IConfiguration configuration, bool debugMode)
    {
        if (debugMode) Console.WriteLine($"{nameof(AddSupportToolsServerForCommandsDatabase)} Started");

        const string connectionStringConfigurationKey = "Data:SupportToolsServerDatabase:ConnectionString";
        var connectionString = configuration[connectionStringConfigurationKey];

        if (string.IsNullOrWhiteSpace(connectionString) && !debugMode)
        {
            Console.WriteLine($"Parameter {connectionStringConfigurationKey} is empty");
            return services;
        }

        services.AddSingleton<IDbConnectionFactory>(_ =>
            new SqlDbConnectionFactory(configuration[connectionStringConfigurationKey]!));

        if (debugMode) Console.WriteLine($"{nameof(AddSupportToolsServerForCommandsDatabase)} Finished");

        return services;
    }
}