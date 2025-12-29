using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SupportToolsServerCommandRepositories.Installers;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerForCommandsDatabaseInstaller
{
    public static IServiceCollection AddSupportToolsServerForCommandsDatabase(this IServiceCollection services,
        IConfiguration configuration, bool debugMode)
    {
        if (debugMode) Console.WriteLine($"{nameof(AddSupportToolsServerForCommandsDatabase)} Started");

        var connectionString = configuration["Data:SupportToolsServerDatabase:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString) && !debugMode)
        {
            Console.WriteLine("SupportToolsServerDatabaseInstaller.InstallServices connectionString is empty");
            return services;
        }

        services.AddSingleton<IDbConnectionFactory>(_ =>
            new SqlDbConnectionFactory(configuration["Data:SupportToolsServerDatabase:ConnectionString"]!));

        if (debugMode) Console.WriteLine($"{nameof(AddSupportToolsServerForCommandsDatabase)} Finished");

        return services;
    }
}