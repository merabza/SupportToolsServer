using Microsoft.AspNetCore.Routing;
using SupportToolsServer.Api.Endpoints.V1;
using System;

namespace SupportToolsServer.Api.DependencyInjection;

public static class SupportToolsServerApiDependencyInjection
{
    public static bool UseSupportToolsServerApi(this IEndpointRouteBuilder endpoints, bool debugMode)
    {
        if (debugMode)
            Console.WriteLine($"{nameof(UseSupportToolsServerApi)} Started");

        endpoints.UseGitIgnoreFileTypesEndpoints(debugMode);

        if (debugMode)
            Console.WriteLine($"{nameof(UseSupportToolsServerApi)} Finished");

        return true;
    }
}