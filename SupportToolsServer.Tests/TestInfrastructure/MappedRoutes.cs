using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        (bool mapped, List<RouteEndpoint> endpoints) = await Map(useEndpoints);
        return (mapped, Describe(endpoints));
    }

    //The routes that require an authenticated user (RequireAuthorization adds IAuthorizeData to their metadata)
    public static async Task<List<string>> RequiringAuthorization(Func<WebApplication, bool> useEndpoints)
    {
        (_, List<RouteEndpoint> endpoints) = await Map(useEndpoints);
        return Describe(endpoints.Where(e =>
            e.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0 &&
            e.Metadata.GetMetadata<IAllowAnonymous>() is null));
    }

    private static async Task<(bool Mapped, List<RouteEndpoint> Endpoints)> Map(Func<WebApplication, bool> useEndpoints)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddApplication(null, typeof(AssemblyReference));
        await using WebApplication app = builder.Build();

        bool mapped = useEndpoints(app);

        List<RouteEndpoint> endpoints =
            [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()];
        return (mapped, endpoints);
    }

    private static List<string> Describe(IEnumerable<RouteEndpoint> endpoints)
    {
        return [.. endpoints.Select(Describe).Order(StringComparer.Ordinal)];
    }

    private static string Describe(RouteEndpoint endpoint)
    {
        string method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Single();
        return $"{method} {endpoint.RoutePattern.RawText!.Trim('/')}";
    }
}
