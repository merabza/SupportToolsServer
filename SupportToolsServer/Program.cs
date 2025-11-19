using System;
using System.Collections.Generic;
using System.Reflection;
using ConfigurationEncrypt;
using Figgle.Fonts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SwaggerTools;
using WebInstallers;
using AssemblyReference = SupportToolsServerDb.AssemblyReference;

try
{
    Console.WriteLine("Loading...");

    const string appName = "Support Tools Server";

    var header = $"{appName} {Assembly.GetEntryAssembly()?.GetName().Version}";
    Console.WriteLine(FiggleFonts.Standard.Render(header));

    var parameters = new Dictionary<string, string>
    {
        { ConfigurationEncryptInstaller.AppKeyKey, "3081adaf7a5d472a88cd5149671a1922" },
        { SwaggerInstaller.AppNameKey, appName },
        { SwaggerInstaller.VersionCountKey, 1.ToString() },
        { SwaggerInstaller.UseSwaggerWithJwtBearerKey, string.Empty } //Allow Swagger
    };

    var builder =
        WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory, Args = args
        });

    var debugMode = builder.Environment.IsDevelopment();

    if (!builder.InstallServices(debugMode, args, parameters,

        // @formatter:off

        //SupportToolsServerDbPart
        AssemblyReference.Assembly, 
        SupportToolsServerRepositories.AssemblyReference.Assembly,
        SupportToolsServerApi.AssemblyReference.Assembly,

        //WebSystemTools
        ApiExceptionHandler.AssemblyReference.Assembly, 
        SupportToolsServerApiKeyIdentity.AssemblyReference.Assembly,
        ConfigurationEncrypt.AssemblyReference.Assembly, 
        SerilogLogger.AssemblyReference.Assembly,
        SignalRMessages.AssemblyReference.Assembly,
        StaticFilesTools.AssemblyReference.Assembly, 
        SwaggerTools.AssemblyReference.Assembly, 
        TestToolsApi.AssemblyReference.Assembly,
        WindowsServiceTools.AssemblyReference.Assembly))

        // @formatter:on

        return 2;

    var mediatRSettings = builder.Configuration.GetSection("MediatRLicenseKey");

    var mediatRLicenseKey = mediatRSettings.Get<string>();

    builder.Services.AddMediatR(cfg =>
    {
        cfg.LicenseKey = mediatRLicenseKey;
        cfg.RegisterServicesFromAssembly(SupportToolsServerApi.AssemblyReference.Assembly);
    });

    //ReSharper disable once using

    using var app = builder.Build();

    if (!app.UseServices(debugMode)) return 1;

    app.Run();
    return 0;
}
catch (Exception e)
{
    Log.Fatal(e, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}