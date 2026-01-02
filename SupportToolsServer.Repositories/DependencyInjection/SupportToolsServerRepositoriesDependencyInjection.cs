using System;
using Microsoft.Extensions.DependencyInjection;
using SupportToolsServer.Domain.GitIgnoreFileTypes;

namespace SupportToolsServer.Repositories.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerRepositoriesDependencyInjection
{
    public static IServiceCollection AddSupportToolsServer_Repositories(this IServiceCollection services,
        bool debugMode)
    {
        if (debugMode)
            Console.WriteLine($"{nameof(AddSupportToolsServer_Repositories)} Started");

        //builder.Services.AddScoped<IGitsQueriesRepository, GitsQueriesRepository>();
        //builder.Services.AddScoped<IGitIgnoreFileTypesQueriesRepository, GitIgnoreFileTypesQueriesRepository>();
        services.AddScoped<IGitIgnoreFileTypeRepository, GitIgnoreFileTypeRepository>();

        if (debugMode)
            Console.WriteLine($"{nameof(AddSupportToolsServer_Repositories)} Finished");

        return services;
    }
}