using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SupportToolsServer.Tests.TestInfrastructure;
using Xunit;

namespace SupportToolsServer.Tests.Host;

//The API key authentication of the real host. Program.cs calls only UseAuthorization, and WebApplication adds
//UseAuthentication by itself: a valid key gets 200 only if the authentication middleware has run before the authorization
public sealed class ApiKeyAuthenticationTests : IClassFixture<SupportToolsServerHostFactory>
{
    private const string GitReposRoute = "api/v1/git/gitrepos";
    private const string NegotiateRoute = "api/v1/messages/negotiate?negotiateVersion=1";

    private readonly SupportToolsServerHostFactory _factory;

    public ApiKeyAuthenticationTests(SupportToolsServerHostFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpStatusCode Status, string Body)> Send(HttpMethod method, string route,
        string? apiKey = null, string? clientAddress = null)
    {
        string address = route;
        if (apiKey is not null)
        {
            char separator = route.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            address = $"{route}{separator}apikey={Uri.EscapeDataString(apiKey)}";
        }

        using var request = new HttpRequestMessage(method, new Uri(address, UriKind.Relative));
        if (clientAddress is not null)
        {
            request.Headers.Add(ClientAddressStartupFilter.ClientAddressHeader, clientAddress);
        }

        using HttpClient client = _factory.CreateClient();
        using HttpResponseMessage response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GitRepos_ReturnsUnauthorized_WithoutApiKey()
    {
        (HttpStatusCode status, _) = await Send(HttpMethod.Get, GitReposRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task GitRepos_ReturnsOk_WithTheApiKeyOfTheClientAddress()
    {
        (HttpStatusCode status, string body) =
            await Send(HttpMethod.Get, GitReposRoute, SupportToolsServerHostFactory.ValidApiKey);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("[]", body);
    }

    [Theory]
    [InlineData("wrong-key")]
    [InlineData("VALID-TEST-KEY")]
    [InlineData("valid-test-key ")]
    [InlineData(" ")]
    public async Task GitRepos_ReturnsUnauthorized_WithAnInvalidApiKey(string apiKey)
    {
        (HttpStatusCode status, _) = await Send(HttpMethod.Get, GitReposRoute, apiKey);

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task GitRepos_ReturnsUnauthorized_WithTheApiKeyOfAnotherAddress()
    {
        (HttpStatusCode status, _) =
            await Send(HttpMethod.Get, GitReposRoute, SupportToolsServerHostFactory.OtherAddressApiKey);

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task GitRepos_ReturnsUnauthorized_WithAValidApiKeyFromAnotherAddress()
    {
        (HttpStatusCode status, _) = await Send(HttpMethod.Get, GitReposRoute,
            SupportToolsServerHostFactory.ValidApiKey, SupportToolsServerHostFactory.OtherAddress);

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task GitRepos_ReturnsOk_WithAnApiKeyFromItsOwnAddress()
    {
        (HttpStatusCode status, _) = await Send(HttpMethod.Get, GitReposRoute,
            SupportToolsServerHostFactory.OtherAddressApiKey, SupportToolsServerHostFactory.OtherAddress);

        Assert.Equal(HttpStatusCode.OK, status);
    }

    [Theory]
    [InlineData(ClientAddressStartupFilter.DefaultClientAddress)]
    [InlineData("192.168.1.7")]
    [InlineData("::ffff:172.16.0.5")]
    public async Task GitRepos_ReturnsOk_WithAnAnyAddressApiKey(string clientAddress)
    {
        (HttpStatusCode status, _) = await Send(HttpMethod.Get, GitReposRoute,
            SupportToolsServerHostFactory.AnyAddressApiKey, clientAddress);

        Assert.Equal(HttpStatusCode.OK, status);
    }

    //One route of every group and method; the authorization refuses the request before the handler runs
    [Theory]
    [InlineData("GET", "api/v1/git/gitrepo/RepoA")]
    [InlineData("POST", "api/v1/git/uploadgitrepos")]
    [InlineData("POST", "api/v1/git/updategitrepo/RepoA")]
    [InlineData("DELETE", "api/v1/git/deletegitrepo/RepoA")]
    [InlineData("GET", "api/v1/git/gitignorefiletypeslist")]
    [InlineData("POST", "api/v1/git/updategitignorefiletype/CSharp")]
    [InlineData("POST", "api/v1/git/syncupgitignorefiletypes")]
    [InlineData("DELETE", "api/v1/git/deletegitignorefiletype/CSharp")]
    [InlineData("GET", "api/v1/git/editorconfigfiletypeslist")]
    [InlineData("POST", "api/v1/git/syncupeditorconfigfiletypes/true")]
    [InlineData("DELETE", "api/v1/git/deleteeditorconfigfiletype/default")]
    [InlineData("POST", NegotiateRoute)]
    public async Task EveryProtectedRoute_ReturnsUnauthorized_WithoutApiKey(string method, string route)
    {
        (HttpStatusCode status, _) = await Send(new HttpMethod(method), route);

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task MessagesHub_AcceptsTheNegotiation_WithApiKey()
    {
        (HttpStatusCode status, _) =
            await Send(HttpMethod.Post, NegotiateRoute, SupportToolsServerHostFactory.ValidApiKey);

        Assert.Equal(HttpStatusCode.OK, status);
    }

    //VersionChecker calls the test routes without a key
    [Theory]
    [InlineData("api/v1/test/getversion")]
    [InlineData("api/v1/test/testconnection")]
    public async Task TestRoutes_ReturnOk_WithoutApiKey(string route)
    {
        (HttpStatusCode status, _) = await Send(HttpMethod.Get, route);

        Assert.Equal(HttpStatusCode.OK, status);
    }

    //A new route group must be created with RequireAuthorization: then only the test routes stay anonymous
    [Fact]
    public void OnlyTheTestRoutes_AreAnonymous()
    {
        var dataSource = _factory.Services.GetRequiredService<EndpointDataSource>();

        List<string> anonymousRoutes =
        [
            .. dataSource.Endpoints.OfType<RouteEndpoint>()
                .Where(e => e.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0 ||
                            e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
                .Select(e => e.RoutePattern.RawText!.Trim('/')).Order(StringComparer.Ordinal)
        ];

        Assert.Equal([
            "api/v1/test/getappsettingsversion", "api/v1/test/getip", "api/v1/test/getsettings",
            "api/v1/test/getversion", "api/v1/test/testconnection"
        ], anonymousRoutes);
    }
}
