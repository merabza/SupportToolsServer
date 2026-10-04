using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace SupportToolsServer.Tests.TestInfrastructure;

//TestServer leaves Connection.RemoteIpAddress null, and the API key handler authenticates no request without it.
//This filter puts a middleware in front of the whole pipeline (before the authentication that WebApplication adds),
//which takes the client address from a test header, or uses the default address
internal sealed class ClientAddressStartupFilter : IStartupFilter
{
    public const string ClientAddressHeader = "X-Test-Client-Address";
    public const string DefaultClientAddress = "10.20.30.40";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                string? address = context.Request.Headers[ClientAddressHeader];
                context.Connection.RemoteIpAddress = IPAddress.Parse(address ?? DefaultClientAddress);
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
