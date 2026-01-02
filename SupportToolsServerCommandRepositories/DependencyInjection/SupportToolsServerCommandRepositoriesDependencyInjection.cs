using System;
using Microsoft.Extensions.DependencyInjection;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Repositories.Gits;

namespace SupportToolsServerCommandRepositories.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerCommandRepositoriesDependencyInjection
{
    public static IServiceCollection AddSupportToolsServerCommandRepositories(this IServiceCollection services,
        bool debugMode)
    {
        if (debugMode)
            Console.WriteLine($"{nameof(AddSupportToolsServerCommandRepositories)} Started");

        services.AddScoped<IGitsCommandsRepository, GitsCommandsRepository>();
        services.AddScoped<IGitIgnoreFileTypesCommandsRepository, GitIgnoreFileTypesCommandsRepository>();

        if (debugMode)
            Console.WriteLine($"{nameof(AddSupportToolsServerCommandRepositories)} Finished");

        return services;
    }
}