using System;
using System.Reflection;
using ApiExceptionHandler.DependencyInjection;
using ApiKeyIdentity.DependencyInjection;
using ConfigurationEncrypt;
using Figgle.Fonts;
using MediatorTools.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using SerilogLogger;
using SignalRMessages.DependencyInjection;
using SignalRMessages.Endpoints.V1;
using StaticFilesTools.DependencyInjection;
using SupportToolsServer.Api.DependencyInjection;
using SupportToolsServer.Application.Data;
using SupportToolsServer.Persistence;
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
    const string appKey = "3081adaf7a5d472a88cd5149671a1922";
    const int versionCount = 1;

    string header = $"{appName} {Assembly.GetEntryAssembly()?.GetName().Version}";
    Console.WriteLine(FiggleFonts.Standard.Render(header));

    WebApplicationBuilder builder =
        WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory, Args = args
        });

    bool debugMode = builder.Environment.IsDevelopment();

    ILogger logger = builder.Host.UseSerilogLogger(debugMode, builder.Configuration);
    ILogger? debugLogger = debugMode ? logger : null;

    builder.Host.UseWindowsServiceOnWindows(debugLogger, args);

    builder.Configuration.AddConfigurationEncryption(debugLogger, appKey);

    // @formatter:off
    builder.Services
        .AddSwagger(debugLogger, true, versionCount, appName) //+
        .AddApiKeyIdentity(debugLogger)
        .AddSignalRMessages(debugLogger)
        .AddSupportToolsServerPersistence(debugLogger, builder.Configuration)
        .AddMediator(debugLogger, builder.Configuration, typeof(ISupportToolsServerDbContext).Assembly)
        //.AddSupportToolsServerApiKeyIdentity(debugMode)
        .AddAllScopedServiceSupportToolsServerApplication()
        .AddSupportToolsServerQueryRepositories(debugLogger)
        .AddSupportToolsServerCommandRepositories(debugLogger)
        .AddSupportToolsServerForCommandsDatabase(debugLogger, builder.Configuration)
        .AddSupportToolsServer_Repositories(debugLogger)
        .AddSupportToolsServerDb(debugLogger, builder.Configuration);
    // @formatter:on

    //ReSharper disable once using
    await using WebApplication app = builder.Build();

    // ReSharper disable once RedundantArgumentDefaultValue
    app.UseSwaggerServices(debugLogger, versionCount); //+
    app.UseSupportToolsServerApi(debugLogger);
    app.UseApiExceptionHandler(debugLogger); //+
    app.UseApiKeysAuthorization(debugLogger);
    app.UseSignalRMessagesHub(debugLogger); //+
    app.UseTestEndpoints(debugLogger); //+
    app.UseDefaultAndStaticFiles(debugLogger); //+

    await app.RunAsync();
    return 0;
}
catch (Exception e)
{
    Log.Fatal(e, "Host terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
