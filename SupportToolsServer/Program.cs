using System;
using System.Reflection;
using Figgle.Fonts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using SupportToolsServer.Application;
using SupportToolsServer.Infrastructure.DependencyInjection;
using SupportToolsServer.WebApi.DependencyInjection;
using SupportToolsServerDbPart.Db.DependencyInjection;
using SystemTools.Application.Abstractions;
using WebSystemTools.ApiExceptionHandler.DependencyInjection;
using WebSystemTools.ApiKeyIdentity.DependencyInjection;
using WebSystemTools.SerilogLogger;
using WebSystemTools.SignalRMessages.DependencyInjection;
using WebSystemTools.SignalRMessages.Endpoints.V1;
using WebSystemTools.StaticFilesTools.DependencyInjection;
using WebSystemTools.SwaggerTools;
using WebSystemTools.SwaggerTools.DependencyInjection;
using WebSystemTools.TestToolsApi.DependencyInjection;
using WebSystemTools.TestToolsApi.Endpoints.V1;
using WebSystemTools.ValidationTools.DependencyInjection;
using WebSystemTools.WindowsServiceTools;

try
{
    Console.WriteLine("Loading...");

    const string appName = "Support Tools Server";
    //const string appKey = "3081adaf7a5d472a88cd5149671a1922";
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

    //builder.Configuration.AddConfigurationEncryption(debugLogger, appKey);

    // @formatter:off
    builder.Services
        .AddSwagger(debugLogger, ESwaggerSecurityScheme.ApiKey, versionCount, appName) //+
        .AddApiKeyIdentity(debugLogger)
        .AddSignalRMessages(debugLogger)
        .AddSupportToolsServerDatabase(debugLogger, builder.Configuration)
        .AddApplication(debugLogger, typeof(AssemblyReference))
        .AddFluentValidation(debugLogger, AssemblyReference.Assembly)
        .AddSupportToolsServerRepositories(debugLogger)
        .AddSupportToolsServerGitProjects(debugLogger, builder.Configuration);
    // @formatter:on

    //ReSharper disable once using
    await using WebApplication app = builder.Build();

    // ReSharper disable once RedundantArgumentDefaultValue
    app.UseSwaggerServices(debugLogger, versionCount); //+
    app.UseSupportToolsServerApi(debugLogger);
    app.UseApiExceptionHandler(debugLogger); //+
    app.UseApiKeysAuthorization(debugLogger);
    app.UseSignalRMessagesHub(debugLogger); //+
    app.UseTestToolsApiEndpoints(debugLogger);
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
