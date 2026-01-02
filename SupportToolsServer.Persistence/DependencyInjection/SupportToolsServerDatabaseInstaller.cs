//using Microsoft.AspNetCore.Builder;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using SupportToolsServer.Application.Data;
//using System;
//using System.Collections.Generic;

//namespace SupportToolsServer.Persistence.DependencyInjection;

//// ReSharper disable once UnusedType.Global
//public sealed class SupportToolsServerDatabaseDependencyInjection
//{
//    public int InstallPriority => 30;
//    public int ServiceUsePriority => 30;

//    public bool AddSupportToolsServer_Persistence(WebApplicationBuilder builder, bool debugMode, string[] args,
//        Dictionary<string, string> parameters)
//    {
//        if (debugMode) Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Started");

//        var connectionString = builder.Configuration["Data:SupportToolsServerDatabase:ConnectionString"];

//        if (string.IsNullOrWhiteSpace(connectionString) && !debugMode)
//        {
//            Console.WriteLine("AddSupportToolsServer_Persistence connectionString is empty");
//            return false;
//        }

//        builder.Services.AddDbContext<SupportToolsServerDbContext>(options => options.UseSqlServer(connectionString));

//        builder.Services.AddScoped<ISupportToolsServerDbContext>(provider => provider.GetRequiredService<SupportToolsServerDbContext>());
//        builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SupportToolsServerDbContext>());




//        if (debugMode) Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Finished");

//        return true;
//    }

//    public bool UseServices(WebApplication app, bool debugMode)
//    {
//        return true;
//    }
//}