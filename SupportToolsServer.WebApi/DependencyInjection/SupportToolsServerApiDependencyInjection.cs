using Microsoft.AspNetCore.Routing;
using Serilog;
using SupportToolsServer.WebApi.Endpoints.V1;

namespace SupportToolsServer.WebApi.DependencyInjection;

public static class SupportToolsServerApiDependencyInjection
{
    public static bool UseSupportToolsServerApi(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseSupportToolsServerApi));

        endpoints.UseGitIgnoreFileTypesEndpoints(debugLogger);
        endpoints.UseGitReposEndpoints(debugLogger);
        endpoints.UseEditorConfigFileTypesEndpoints(debugLogger);
        endpoints.UseEnvironmentsEndpoints(debugLogger);
        endpoints.UseRuntimesEndpoints(debugLogger);
        endpoints.UseNpmPackagesEndpoints(debugLogger);
        endpoints.UseReactAppTemplatesEndpoints(debugLogger);
        endpoints.UseDotnetToolsEndpoints(debugLogger);
        endpoints.UseSmartSchemasEndpoints(debugLogger);
        endpoints.UseFileStoragesEndpoints(debugLogger);
        endpoints.UseApiClientsEndpoints(debugLogger);
        endpoints.UseDatabaseServerConnectionsEndpoints(debugLogger);
        endpoints.UseServersEndpoints(debugLogger);

        debugLogger?.Information("{MethodName} Finished", nameof(UseSupportToolsServerApi));

        return true;
    }
}
