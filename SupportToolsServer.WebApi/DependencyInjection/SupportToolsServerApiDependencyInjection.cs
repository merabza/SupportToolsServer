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

        debugLogger?.Information("{MethodName} Finished", nameof(UseSupportToolsServerApi));

        return true;
    }
}
