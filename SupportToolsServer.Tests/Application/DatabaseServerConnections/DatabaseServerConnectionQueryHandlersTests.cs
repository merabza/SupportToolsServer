using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.DatabaseServerConnections;
using SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnectionByName;
using SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnections;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.DatabaseServerConnections;

public sealed class DatabaseServerConnectionQueryHandlersTests
{
    private readonly Mock<IApiClientRepository> _apiClients = new();
    private readonly Mock<IDatabaseServerConnectionRepository> _connections = new();
    private readonly ApiClient _webAgent = TestData.NewApiClient("Pc1.WebAgent");

    public DatabaseServerConnectionQueryHandlersTests()
    {
        _apiClients.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewApiClient("Pc2.WebAgent"), _webAgent]);
    }

    private Task<Result<StsDatabaseServerConnectionDataModel>> GetByName(string name)
    {
        return new GetDatabaseServerConnectionByNameQueryHandler(_connections.Object, _apiClients.Object).Handle(
            new GetDatabaseServerConnectionByNameQuery(name), CancellationToken.None);
    }

    //The order ignores case, so it differs from the ordinal one; the web agent is named, not given by its id
    [Fact]
    public async Task GetDatabaseServerConnections_ReturnsEveryRecordWithTheWebAgentNameInNameOrder()
    {
        _connections.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewDatabaseServerConnection("Pc2.Sql", version: 2),
            TestData.NewDatabaseServerConnection("archive.Sql", _webAgent, ["Default"], 4),
            TestData.NewDatabaseServerConnection("Pc1.Sql", _webAgent)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsDatabaseServerConnectionDataModel>> result =
            await new GetDatabaseServerConnectionsQueryHandler(_connections.Object, _apiClients.Object).Handle(
                new GetDatabaseServerConnectionsQuery(), cancellation.Token);

        Assert.Equal(["archive.Sql", "Pc1.Sql", "Pc2.Sql"], result.Value.Select(x => x.Name));
        Assert.Equal(["Pc1.WebAgent", "Pc1.WebAgent", null], result.Value.Select(x => x.DbWebAgentName));
        Assert.Equal([1, 0, 0], result.Value.Select(x => x.DatabaseFoldersSets.Count));
        Assert.Equal([4, 1, 2], result.Value.Select(x => x.Version));
        _connections.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _apiClients.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetDatabaseServerConnectionByName_ReturnsTheRecordWithItsWebAgentNameAndVersion()
    {
        _connections.Setup(r => r.GetByName("PC1.SQL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewDatabaseServerConnection("Pc1.Sql", _webAgent, ["Default"], 5));

        Result<StsDatabaseServerConnectionDataModel> result = await GetByName("PC1.SQL");

        Assert.Equal("Pc1.Sql", result.Value.Name);
        Assert.Equal("Pc1.WebAgent", result.Value.DbWebAgentName);
        Assert.Equal(TestData.MadeUpPassword, result.Value.ServerPass);
        Assert.Equal("Default", Assert.Single(result.Value.DatabaseFoldersSets).Name);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetDatabaseServerConnectionByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _connections.Setup(r => r.GetByName("Pc2.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatabaseServerConnection?)null);

        Result<StsDatabaseServerConnectionDataModel> result = await GetByName("Pc2.Sql");

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("DatabaseServerConnection With Name Pc2.Sql Not Found", result.Error.Description);
    }

    //The folders sets of the client are a dictionary, so the contract sorts them by name, ignoring case, for a stable
    //hash on the client
    [Fact]
    public void ToContractModel_CopiesEveryFieldWithTheFoldersSetsInNameOrder()
    {
        DatabaseServerConnection connection =
            TestData.NewDatabaseServerConnection("Pc1.Sql", _webAgent, ["Second", "default", "Archive"], 7);

        StsDatabaseServerConnectionDataModel model =
            connection.ToContractModel(new Dictionary<ApiClientId, string> { [_webAgent.Id] = "Pc1.WebAgent" });

        Assert.Equal("Pc1.Sql", model.Name);
        Assert.Equal("SqlServer", model.DatabaseServerProvider);
        Assert.Equal("Pc1.WebAgent", model.DbWebAgentName);
        Assert.Equal("Main", model.RemoteDbConnectionName);
        Assert.Equal("pc1", model.ServerAddress);
        Assert.True(model.WindowsNtIntegratedSecurity);
        Assert.Equal(TestData.MadeUpUser, model.ServerUser);
        Assert.Equal(TestData.MadeUpPassword, model.ServerPass);
        Assert.True(model.TrustServerCertificate);
        Assert.Equal(30, model.ConnectionTimeOut);
        Assert.True(model.Encrypt);
        Assert.Equal(["Archive", "default", "Second"], model.DatabaseFoldersSets.Select(x => x.Name));
        Assert.Equal(@"D:\Archive\Bak", model.DatabaseFoldersSets[0].Backup);
        Assert.Equal(@"D:\Archive\Data", model.DatabaseFoldersSets[0].Data);
        Assert.Equal(@"D:\Archive\Log", model.DatabaseFoldersSets[0].DataLog);
        Assert.Equal(7, model.Version);
    }
}
