using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Host;

//The names of GET git/gitremoteheads go in the query, one value each, escaped by the real client of SupportTools and
//bound to string[] by the real pipeline. The handler of the factory is a stub that returns the names it received
public sealed class GitRemoteHeadNamesTests : IClassFixture<SupportToolsServerHostFactory>
{
    private readonly SupportToolsServerHostFactory _factory;

    public GitRemoteHeadNamesTests(SupportToolsServerHostFactory factory)
    {
        _factory = factory;
    }

    private SupportToolsServerApiClient CreateApiClient()
    {
        return new SupportToolsServerApiClient(null, new TestServerHttpClientFactory(_factory),
            "http://localhost/api/v1", SupportToolsServerHostFactory.ValidApiKey, false);
    }

    [Fact]
    public async Task GetGitRemoteHeads_GivesTheHandlerTheNamesThatTheClientEscaped()
    {
        List<string> names = ["RepoA", "Repo X/1", "a&b=c", "ქართული"];

        Result<List<StsGitRemoteHeadDataModel>> result = await CreateApiClient().GetGitRemoteHeads(names);

        Assert.True(result.IsSuccess);
        Assert.Equal(names, result.Value.Select(x => x.GitName));
    }

    [Fact]
    public async Task GetGitRemoteHeads_GivesTheHandlerNoName_WhenTheClientAsksForEveryGit()
    {
        Result<List<StsGitRemoteHeadDataModel>> result = await CreateApiClient().GetGitRemoteHeads();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    private sealed class TestServerHttpClientFactory : IHttpClientFactory
    {
        private readonly SupportToolsServerHostFactory _factory;

        public TestServerHttpClientFactory(SupportToolsServerHostFactory factory)
        {
            _factory = factory;
        }

        public HttpClient CreateClient(string name)
        {
            return _factory.CreateClient();
        }
    }
}
