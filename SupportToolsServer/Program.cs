using System;
using System.Reflection;
using ApiExceptionHandler.DependencyInjection;
using ApiKeyIdentity.DependencyInjection;
using ConfigurationEncrypt;
using Figgle.Fonts;
using MediatorTools.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SerilogLogger;
using SignalRMessages.DependencyInjection;
using SignalRMessages.Endpoints.V1;
using StaticFilesTools.DependencyInjection;
using SupportToolsServer.Api.DependencyInjection;
using SupportToolsServer.Application.Data;
using SupportToolsServer.Persistence;
using SupportToolsServer.Repositories;
using SupportToolsServer.Repositories.DependencyInjection;
using SupportToolsServerApplication;
using SupportToolsServerCommandRepositories.DependencyInjection;
using SupportToolsServerDb.DependencyInjection;
using SupportToolsServerQueryRepositories.DependencyInjection;
using SwaggerTools.DependencyInjection;
using TestToolsApi.Endpoints.V1;
using WindowsServiceTools;

try
{
    Console.WriteLine("Loading...");

    const string appName = "Support Tools Server";
    const int versionCount = 1;

    var header = $"{appName} {Assembly.GetEntryAssembly()?.GetName().Version}";
    Console.WriteLine(FiggleFonts.Standard.Render(header));

    var builder =
        WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory, Args = args
        });

    var debugMode = builder.Environment.IsDevelopment();

    builder.Host.UseSerilogLogger(builder.Configuration, debugMode); //+
    builder.Host.UseWindowsServiceOnWindows(debugMode, args); //+

    builder.Configuration.AddConfigurationEncryption(debugMode, "3081adaf7a5d472a88cd5149671a1922"); //+

    // @formatter:off
    builder.Services
        .AddSwagger(debugMode, true, versionCount, appName) //+
        .AddApiKeyIdentity(debugMode)
        .AddSignalRMessages(debugMode)
        .AddSupportToolsServerRepositories(debugMode)
        .AddSupportToolsServerPersistence(builder.Configuration, debugMode)
        .AddMediator(builder.Configuration, debugMode, typeof(ISupportToolsServerDbContext).Assembly)
        //.AddSupportToolsServerApiKeyIdentity(debugMode)
        .AddAllScopedServiceSupportToolsServerApplication()
        .AddSupportToolsServerQueryRepositories(debugMode)
        .AddSupportToolsServerCommandRepositories(debugMode)
        .AddSupportToolsServerForCommandsDatabase(builder.Configuration, debugMode)
        .AddSupportToolsServer_Repositories(debugMode)
        .AddSupportToolsServerDb(builder.Configuration, debugMode);
    // @formatter:on

    //ReSharper disable once using
    using var app = builder.Build();

    // ReSharper disable once RedundantArgumentDefaultValue
    app.UseSwaggerServices(debugMode, versionCount); //+
    app.UseSupportToolsServerApi(debugMode);
    app.UseApiExceptionHandler(debugMode); //+
    app.UseApiKeysAuthorization(debugMode);
    app.UseSignalRMessagesHub(debugMode); //+
    app.UseTestEndpoints(debugMode); //+
    app.UseDefaultAndStaticFiles(debugMode); //+

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