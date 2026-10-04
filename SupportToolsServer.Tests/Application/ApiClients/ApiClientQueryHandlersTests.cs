using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.ApiClients.GetApiClientByName;
using SupportToolsServer.Application.ApiClients.GetApiClients;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ApiClients;

public sealed class ApiClientQueryHandlersTests
{
    private readonly Mock<IApiClientRepository> _apiClients = new();

    private Task<Result<StsApiClientDataModel>> GetByName(string name)
    {
        return new GetApiClientByNameQueryHandler(_apiClients.Object).Handle(new GetApiClientByNameQuery(name),
            CancellationToken.None);
    }

    //The order ignores case, so it differs from the ordinal one
    [Fact]
    public async Task GetApiClients_ReturnsEveryFieldAndTheVersionInNameOrder()
    {
        _apiClients.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewApiClient("Pc2.WebAgent", null, null),
            TestData.NewApiClient("bagetter", "https://bagetter.example.com/api/v1/", "key-b", 4),
            TestData.NewApiClient("Pc1.WebAgent", version: 2)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsApiClientDataModel>> result =
            await new GetApiClientsQueryHandler(_apiClients.Object).Handle(new GetApiClientsQuery(),
                cancellation.Token);

        Assert.Equal(["bagetter", "Pc1.WebAgent", "Pc2.WebAgent"], result.Value.Select(x => x.Name));
        Assert.Equal(["https://bagetter.example.com/api/v1/", "http://localhost:5031/api/v1/", null],
            result.Value.Select(x => x.Server));
        Assert.Equal(["key-b", TestData.MadeUpApiKey, null], result.Value.Select(x => x.ApiKey));
        Assert.Equal([4, 2, 1], result.Value.Select(x => x.Version));
        _apiClients.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetApiClientByName_ReturnsTheRecordWithItsVersion()
    {
        _apiClients.Setup(r => r.GetByName("PC1.WEBAGENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewApiClient("Pc1.WebAgent", version: 5));

        Result<StsApiClientDataModel> result = await GetByName("PC1.WEBAGENT");

        Assert.Equal("Pc1.WebAgent", result.Value.Name);
        Assert.Equal("http://localhost:5031/api/v1/", result.Value.Server);
        Assert.Equal(TestData.MadeUpApiKey, result.Value.ApiKey);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetApiClientByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _apiClients.Setup(r => r.GetByName("Pc2.WebAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiClient?)null);

        Result<StsApiClientDataModel> result = await GetByName("Pc2.WebAgent");

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ApiClient With Name Pc2.WebAgent Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        StsApiClientDataModel model =
            TestData.NewApiClient("Pc1.WebAgent", "https://pc1.example.com/api/v1/", "key-a", 7).ToContractModel();

        Assert.Equal("Pc1.WebAgent", model.Name);
        Assert.Equal("https://pc1.example.com/api/v1/", model.Server);
        Assert.Equal("key-a", model.ApiKey);
        Assert.Equal(7, model.Version);
    }

    [Fact]
    public void ToNamesById_MapsTheIdOfEveryApiClientToItsName()
    {
        ApiClient first = TestData.NewApiClient("Pc1.WebAgent");
        ApiClient second = TestData.NewApiClient("Pc2.WebAgent");

        Dictionary<ApiClientId, string> names = new[] { first, second }.ToNamesById();

        Assert.Equal(2, names.Count);
        Assert.Equal("Pc1.WebAgent", names[first.Id]);
        Assert.Equal("Pc2.WebAgent", names[second.Id]);
    }
}
