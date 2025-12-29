//Created by CarcassRepositoriesInstallerClassCreator at 8/1/2022 9:35:56 PM

using System;
using Microsoft.Extensions.DependencyInjection;
using SupportToolsServerApiKeyIdentity;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Repositories.Gits;

//using WebInstallers;

namespace SupportToolsServerQueryRepositories.Installers;

// ReSharper disable once UnusedType.Global
public static class RepositoriesInstaller // : IInstaller
{
    //public int InstallPriority => 30;
    //public int ServiceUsePriority => 30;

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

    //public bool UseServices(WebApplication app, bool debugMode)
    //{
    //    return true;
    //}
}