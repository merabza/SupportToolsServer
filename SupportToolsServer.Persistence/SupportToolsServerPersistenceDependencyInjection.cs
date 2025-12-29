using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportToolsServer.Application.Data;

namespace SupportToolsServer.Persistence;

public static class SupportToolsServerPersistenceDependencyInjection
{
    public static IServiceCollection AddSupportToolsServerPersistence(this IServiceCollection services,
        IConfiguration configuration, bool debugMode)
    {
        if (debugMode) Console.WriteLine($"{nameof(AddSupportToolsServerPersistence)} Started");

        var connectionString = configuration["Data:SupportToolsServerDatabase:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString) && !debugMode)
        {
            Console.WriteLine("SupportToolsServerDatabaseInstaller.InstallServices connectionString is empty");
            throw new InvalidOperationException("Connection string is empty");
        }

        services.AddDbContext<SupportToolsServerDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<ISupportToolsServerDbContext>(provider =>
            provider.GetRequiredService<SupportToolsServerDbContext>());
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SupportToolsServerDbContext>());

        if (debugMode) Console.WriteLine($"{nameof(AddSupportToolsServerPersistence)} Finished");
        return services;
    }
}