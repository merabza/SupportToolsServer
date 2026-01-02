using System;
using Microsoft.Extensions.DependencyInjection;
using SupportToolsServerApiKeyIdentity;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Repositories.Gits;

namespace SupportToolsServerQueryRepositories.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerQueryRepositoriesDependencyInjection
{
    public static IServiceCollection AddSupportToolsServerQueryRepositories(this IServiceCollection services, bool debugMode)
    {
        if (debugMode)
            Console.WriteLine($"{nameof(AddSupportToolsServerQueryRepositories)} Started");

        services.AddScoped<IGitsQueriesRepository, GitsQueriesRepository>();
        services.AddScoped<IGitIgnoreFileTypesQueriesRepository, GitIgnoreFileTypesQueriesRepository>();
        services.AddScoped<IApiKeyQueriesRepository, ApiKeyQueriesRepository>();

        if (debugMode)
            Console.WriteLine($"{nameof(AddSupportToolsServerQueryRepositories)} Finished");

        return services;
    }
}