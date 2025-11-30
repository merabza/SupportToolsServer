//Created by DatabaseInstallerClassCreator at 2/4/2025 7:31:10 PM

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebInstallers;

namespace SupportToolsServerCommandRepositories.Installers;

// ReSharper disable once UnusedType.Global
public sealed class SupportToolsServerForCommandsDatabaseInstaller : IInstaller
{
    public int InstallPriority => 30;
    public int ServiceUsePriority => 30;

    public bool InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args,
        Dictionary<string, string> parameters)
    {
        if (debugMode) Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Started");

        var connectionString = builder.Configuration["Data:SupportToolsServerDatabase:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString) && !debugMode)
        {
            Console.WriteLine("SupportToolsServerDatabaseInstaller.InstallServices connectionString is empty");
            return false;
        }

        builder.Services.AddSingleton<IDbConnectionFactory>(_ =>
            new SqlDbConnectionFactory(builder.Configuration["Data:SupportToolsServerDatabase:ConnectionString"]!));

        if (debugMode) Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Finished");

        return true;
    }

    public bool UseServices(WebApplication app, bool debugMode)
    {
        return true;
    }
}