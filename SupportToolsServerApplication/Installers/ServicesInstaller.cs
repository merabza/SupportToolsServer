////Created by CarcassRepositoriesInstallerClassCreator at 8/1/2022 9:35:56 PM

//using System;
//using System.Collections.Generic;
//using Microsoft.AspNetCore.Builder;
////using WebInstallers;

//namespace SupportToolsServerApplication.Installers;

//// ReSharper disable once UnusedType.Global
//public sealed class ServicesInstaller// : IInstaller
//{
//    public int InstallPriority => 30;
//    public int ServiceUsePriority => 30;

//    public bool InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args,
//        Dictionary<string, string> parameters)
//    {
//        if (debugMode)
//            Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Started");

//        builder.Services.AddAllScopedServiceSupportToolsServerApplication(AssemblyReference.Assembly);

//        if (debugMode)
//            Console.WriteLine($"{GetType().Name}.{nameof(InstallServices)} Finished");

//        return true;
//    }

//    public bool UseServices(WebApplication app, bool debugMode)
//    {
//        return true;
//    }
//}