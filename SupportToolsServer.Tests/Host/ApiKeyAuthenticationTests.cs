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
    [InlineData("GET", "api/v1/git/gitprojects")]
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
    [InlineData("GET", "api/v1/environments")]
    [InlineData("GET", "api/v1/environments/Prod")]
    [InlineData("POST", "api/v1/environments/update/Prod")]
    [InlineData("DELETE", "api/v1/environments/delete/Prod?version=1")]
    [InlineData("GET", "api/v1/runtimes")]
    [InlineData("GET", "api/v1/runtimes/win-x64")]
    [InlineData("POST", "api/v1/runtimes/update/win-x64")]
    [InlineData("DELETE", "api/v1/runtimes/delete/win-x64?version=1")]
    [InlineData("GET", "api/v1/npmpackages")]
    [InlineData("GET", "api/v1/npmpackages/%40reduxjs%2Ftoolkit")]
    [InlineData("POST", "api/v1/npmpackages/update/%40reduxjs%2Ftoolkit")]
    [InlineData("DELETE", "api/v1/npmpackages/delete/%40reduxjs%2Ftoolkit?version=1")]
    [InlineData("GET", "api/v1/reactapptemplates")]
    [InlineData("GET", "api/v1/reactapptemplates/ReduxApp")]
    [InlineData("POST", "api/v1/reactapptemplates/update/ReduxApp")]
    [InlineData("DELETE", "api/v1/reactapptemplates/delete/ReduxApp?version=1")]
    [InlineData("GET", "api/v1/dotnettools")]
    [InlineData("GET", "api/v1/dotnettools/DotnetEf")]
    [InlineData("POST", "api/v1/dotnettools/update/DotnetEf")]
    [InlineData("DELETE", "api/v1/dotnettools/delete/DotnetEf?version=1")]
    [InlineData("GET", "api/v1/smartschemas")]
    [InlineData("GET", "api/v1/smartschemas/Reduce")]
    [InlineData("POST", "api/v1/smartschemas/update/Reduce")]
    [InlineData("DELETE", "api/v1/smartschemas/delete/Reduce?version=1")]
    [InlineData("GET", "api/v1/filestorages")]
    [InlineData("GET", "api/v1/filestorages/Exchange")]
    [InlineData("POST", "api/v1/filestorages/update/Exchange")]
    [InlineData("DELETE", "api/v1/filestorages/delete/Exchange?version=1")]
    [InlineData("GET", "api/v1/apiclients")]
    [InlineData("GET", "api/v1/apiclients/Pc1.WebAgent")]
    [InlineData("POST", "api/v1/apiclients/update/Pc1.WebAgent")]
    [InlineData("DELETE", "api/v1/apiclients/delete/Pc1.WebAgent?version=1")]
    [InlineData("GET", "api/v1/databaseserverconnections")]
    [InlineData("GET", "api/v1/databaseserverconnections/Pc1.Sql")]
    [InlineData("POST", "api/v1/databaseserverconnections/update/Pc1.Sql")]
    [InlineData("DELETE", "api/v1/databaseserverconnections/delete/Pc1.Sql?version=1")]
    [InlineData("GET", "api/v1/servers")]
    [InlineData("GET", "api/v1/servers/dl360")]
    [InlineData("POST", "api/v1/servers/update/dl360")]
    [InlineData("DELETE", "api/v1/servers/delete/dl360?version=1")]
    [InlineData("GET", "api/v1/settings/global")]
    [InlineData("POST", "api/v1/settings/global/update")]
    [InlineData("GET", "api/v1/settings/projectcreator")]
    [InlineData("POST", "api/v1/settings/projectcreator/update")]
    [InlineData("GET", "api/v1/projecttemplates")]
    [InlineData("GET", "api/v1/projecttemplates/Console%20With%20Database")]
    [InlineData("POST", "api/v1/projecttemplates/update/Console%20With%20Database")]
    [InlineData("DELETE", "api/v1/projecttemplates/delete/Console%20With%20Database?version=1")]
    [InlineData("GET", "api/v1/projects")]
    [InlineData("GET", "api/v1/projects/AppA")]
    [InlineData("POST", "api/v1/projects/update/AppA")]
    [InlineData("DELETE", "api/v1/projects/delete/AppA?version=1")]
    [InlineData("GET", "api/v1/files")]
    [InlineData("GET", "api/v1/files/content?path=D%3A%5C1WorkSecurity%5Cappsettings.json")]
    [InlineData("POST", "api/v1/files/update")]
    [InlineData("DELETE", "api/v1/files/delete?path=D%3A%5C1WorkSecurity%5Cappsettings.json&version=1")]
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
