using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SupportToolsServer.Application.Data;

namespace SupportToolsServer.Persistence;

public static class SupportToolsServerPersistenceDependencyInjection
{
    public static IServiceCollection AddSupportToolsServerPersistence(this IServiceCollection services,
        ILogger? debugLogger, IConfiguration configuration)
    {
        debugLogger?.Information("{MethodName} Started", nameof(AddSupportToolsServerPersistence));

        const string connectionStringConfigurationKey = "Data:SupportToolsServerDatabase:ConnectionString";
        string? connectionString = configuration[connectionStringConfigurationKey];

        if (string.IsNullOrWhiteSpace(connectionString) && debugLogger is not null)
        {
            debugLogger.Error("Parameter {ConnectionStringConfigurationKey} is empty",
                connectionStringConfigurationKey);
            throw new InvalidOperationException("Connection string is empty");
        }

        services.AddDbContext<SupportToolsServerDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<ISupportToolsServerDbContext>(provider =>
            provider.GetRequiredService<SupportToolsServerDbContext>());

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerPersistence));

        return services;
    }
}
