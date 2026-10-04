using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.Servers;
using SupportToolsServer.Application.Servers.GetServerByName;
using SupportToolsServer.Application.Servers.GetServers;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Servers;

public sealed class ServerQueryHandlersTests
{
    private readonly Mock<IApiClientRepository> _apiClients = new();
    private readonly Runtime _runtime = TestData.NewRuntime("linux-x64");
    private readonly Mock<IRuntimeRepository> _runtimes = new();
    private readonly Mock<IServerRepository> _servers = new();
    private readonly ApiClient _webAgent = TestData.NewApiClient("Dl360.WebAgent");
    private readonly ApiClient _webAgentInstaller = TestData.NewApiClient("Dl360.Installer");

    public ServerQueryHandlersTests()
    {
        _apiClients.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_webAgentInstaller, TestData.NewApiClient("Pc2.WebAgent"), _webAgent]);
        _runtimes.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewRuntime("win-x64"), _runtime]);
    }

    private Task<Result<StsServerDataModel>> GetByName(string name)
    {
        return new GetServerByNameQueryHandler(_servers.Object, _apiClients.Object, _runtimes.Object).Handle(
            new GetServerByNameQuery(name), CancellationToken.None);
    }

    //The order ignores case, so it differs from the ordinal one; the references are named, not given by their ids
    [Fact]
    public async Task GetServers_ReturnsEveryRecordWithTheNamesOfItsReferencesInNameOrder()
    {
        _servers.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewServer("PAZISI", version: 2),
            TestData.NewServer("dl360", _webAgent, _webAgentInstaller, _runtime, 4),
            TestData.NewServer("bee", _webAgent)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsServerDataModel>> result =
            await new GetServersQueryHandler(_servers.Object, _apiClients.Object, _runtimes.Object).Handle(
                new GetServersQuery(), cancellation.Token);

        Assert.Equal(["bee", "dl360", "PAZISI"], result.Value.Select(x => x.Name));
        Assert.Equal(["Dl360.WebAgent", "Dl360.WebAgent", null], result.Value.Select(x => x.WebAgentName));
        Assert.Equal([null, "Dl360.Installer", null], result.Value.Select(x => x.WebAgentInstallerName));
        Assert.Equal([null, "linux-x64", null], result.Value.Select(x => x.Runtime));
        Assert.Equal([1, 4, 2], result.Value.Select(x => x.Version));
        _servers.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _apiClients.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _runtimes.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetServerByName_ReturnsTheRecordWithTheNamesOfItsReferencesAndItsVersion()
    {
        _servers.Setup(r => r.GetByName("DL360", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewServer("dl360", _webAgent, _webAgentInstaller, _runtime, 5));

        Result<StsServerDataModel> result = await GetByName("DL360");

        Assert.Equal("dl360", result.Value.Name);
        Assert.Equal("Dl360.WebAgent", result.Value.WebAgentName);
        Assert.Equal("Dl360.Installer", result.Value.WebAgentInstallerName);
        Assert.Equal("linux-x64", result.Value.Runtime);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetServerByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _servers.Setup(r => r.GetByName("guria", It.IsAny<CancellationToken>())).ReturnsAsync((Server?)null);

        Result<StsServerDataModel> result = await GetByName("guria");

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Server With Name guria Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        Server server = TestData.NewServer("dl360", _webAgent, _webAgentInstaller, _runtime, 7);

        StsServerDataModel model = server.ToContractModel(
            new Dictionary<ApiClientId, string>
            {
                [_webAgent.Id] = "Dl360.WebAgent", [_webAgentInstaller.Id] = "Dl360.Installer"
            }, new Dictionary<RuntimeId, string> { [_runtime.Id] = "linux-x64" });

        Assert.Equal("dl360", model.Name);
        Assert.Equal("Dl360.WebAgent", model.WebAgentName);
        Assert.Equal("Dl360.Installer", model.WebAgentInstallerName);
        Assert.Equal("deployer", model.FilesUserName);
        Assert.Equal("deployers", model.FilesUsersGroupName);
        Assert.Equal("linux-x64", model.Runtime);
        Assert.Equal("/home/deployer/Download", model.ServerSideDownloadFolder);
        Assert.Equal("/opt/apps", model.ServerSideDeployFolder);
        Assert.Equal(7, model.Version);
    }
}
