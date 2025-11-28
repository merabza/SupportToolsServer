//Created by CarcassRepositoriesInstallerClassCreator at 8/1/2022 9:35:56 PM

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RepositoriesDom;
using SupportToolsServerDom;
using System;
using System.Collections.Generic;
using System.Linq;
using WebInstallers;

namespace SupportToolsServerRepositories.Installers;

// ReSharper disable once UnusedType.Global
public sealed class RepositoriesInstaller : IInstaller
{
    public int InstallPriority => 30;
    public int ServiceUsePriority => 30;

    public bool InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args,
        Dictionary<string, string> parameters)
    {
        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Started");

        builder.Services.AddScoped<IGitsQueriesRepository, GitsQueriesRepository>();
        builder.Services.AddScoped<IGitsCommandsRepository, GitsCommandsRepository>();

        builder.Services.AddScoped<IGitIgnoreFileTypesQueriesRepository, GitIgnoreFileTypesQueriesRepository>();
        builder.Services.AddScoped<IGitIgnoreFileTypesCommandsRepository, GitIgnoreFileTypesCommandsRepository>();

        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Finished");

        return true;
    }

    public bool UseServices(WebApplication app, bool debugMode)
    {
        return true;
    }
}