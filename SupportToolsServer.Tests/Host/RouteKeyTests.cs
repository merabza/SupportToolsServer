using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Host;

//The registry key of the real routing pipeline. The client escapes the key with Uri.EscapeDataString; ASP.NET Core
//unescapes the route value except %2F, which the registry endpoints turn back into a slash (RouteKeys).
//GET environments/{key} of the factory is a stub that returns the name it received
public sealed class RouteKeyTests : IClassFixture<SupportToolsServerHostFactory>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SupportToolsServerHostFactory _factory;

    public RouteKeyTests(SupportToolsServerHostFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Prod")]
    [InlineData("Pre Prod")]
    [InlineData("@scope/name")]
    [InlineData("a+b&c=d")]
    public async Task GetEnvironment_GivesTheHandlerTheNameThatTheClientEscaped(string name)
    {
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri(
            $"api/v1/environments/{Uri.EscapeDataString(name)}?apikey={SupportToolsServerHostFactory.ValidApiKey}",
            UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var environment =
            JsonSerializer.Deserialize<StsEnvironmentDataModel>(await response.Content.ReadAsStringAsync(),
                WebJsonOptions);
        Assert.NotNull(environment);
        Assert.Equal(name, environment.Name);
    }
}
