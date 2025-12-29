using System;
using Microsoft.Extensions.DependencyInjection;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Repositories.Gits;

namespace SupportToolsServerCommandRepositories.Installers;

// ReSharper disable once UnusedType.Global
public static class RepositoriesInstaller
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