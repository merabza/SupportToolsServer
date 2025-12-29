using Microsoft.Extensions.DependencyInjection;
using SupportToolsServer.Domain.GitIgnoreFileTypes;
using System;

namespace SupportToolsServer.Repositories;

public static class SupportToolsServerRepositoriesDependencyInjection
{
    public static IServiceCollection AddSupportToolsServerRepositories(this IServiceCollection services, bool debugMode)
    {
        if (debugMode)
            Console.WriteLine($"{nameof(AddSupportToolsServerRepositories)} Started");

        //builder.Services.AddScoped<IGitsQueriesRepository, GitsQueriesRepository>();
        //builder.Services.AddScoped<IGitIgnoreFileTypesQueriesRepository, GitIgnoreFileTypesQueriesRepository>();
        services.AddScoped<IGitIgnoreFileTypeRepository, GitIgnoreFileTypeRepository>();

        if (debugMode)
            Console.WriteLine($"{nameof(AddSupportToolsServerRepositories)} Finished");

        return services;
    }
}