using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SupportToolsServer.Application;
using SystemTools.Application.Abstractions;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Maps endpoints on a real WebApplication and describes every route as "METHOD pattern", in ordinal order.
//The handlers are registered, so the minimal API can infer which endpoint parameters are services
internal static class MappedRoutes
{
    public static async Task<(bool Mapped, List<string> Routes)> Of(Func<WebApplication, bool> useEndpoints)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddApplication(null, typeof(AssemblyReference));
        await using WebApplication app = builder.Build();

        bool mapped = useEndpoints(app);

        List<string> routes =
        [
            .. ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Select(Describe).Order(StringComparer.Ordinal)
        ];
        return (mapped, routes);
    }

    private static string Describe(RouteEndpoint endpoint)
    {
        string method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Single();
        return $"{method} {endpoint.RoutePattern.RawText!.Trim('/')}";
    }
}
