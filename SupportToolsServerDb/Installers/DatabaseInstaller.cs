//Created by DatabaseInstallerClassCreator at 2/4/2025 7:31:10 PM

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebInstallers;

namespace SupportToolsServerDb.Installers;

// ReSharper disable once UnusedType.Global
public sealed class SupportToolsServerDatabaseInstaller : IInstaller
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

        builder.Services.AddDbContext<SupportToolsServerDbContext>(options => options.UseSqlServer(connectionString));
        if (debugMode) Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Finished");

        return true;
    }

    public bool UseServices(WebApplication app, bool debugMode)
    {
        return true;
    }
}