using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.ApiClients.UpdateApiClient;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ApiClients;

public sealed class UpdateApiClientCommandHandlerTests
{
    private readonly Mock<IApiClientRepository> _apiClients = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void GivenStored(string name, ApiClient? stored)
    {
        _apiClients.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsApiClientDataModel model, CancellationToken cancellationToken = default)
    {
        var handler = new UpdateApiClientCommandHandler(_apiClients.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateApiClientCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _apiClients.Verify(r => r.Add(It.IsAny<ApiClient>()), Times.Never);
        _apiClients.Verify(r => r.Update(It.IsAny<ApiClient>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithTheFirstVersion_WhenVersionZeroIsExpected()
    {
        GivenStored("Pc1.WebAgent", null);
        ApiClient? added = null;
        _apiClients.Setup(r => r.Add(It.IsAny<ApiClient>())).Callback<ApiClient>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle(TestData.ApiClientModel("Pc1.WebAgent"), cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("Pc1.WebAgent", added.Name);
        Assert.Equal("http://localhost:5031/api/v1/", added.Server);
        Assert.Equal(TestData.MadeUpApiKey, added.ApiKey);
        Assert.Equal(1, added.Version);
        _apiClients.Verify(r => r.Update(It.IsAny<ApiClient>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("Pc1.WebAgent", TestData.NewApiClient("PC1.WEBAGENT", version: 2));

        Result<int> result = await Handle(TestData.ApiClientModel("Pc1.WebAgent"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("ApiClient Pc1.WebAgent Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The name is matched without case, so the update also takes the spelling of the route key
    [Fact]
    public async Task Handle_UpdatesTheStoredRecordAndReturnsItsNextVersion_WhenTheExpectedVersionIsStored()
    {
        ApiClient stored = TestData.NewApiClient("Pc1.WebAgent", version: 3);
        GivenStored("PC1.WEBAGENT", stored);

        Result<int> result = await Handle(TestData.ApiClientModel("PC1.WEBAGENT", null, null, 3));

        Assert.Equal(4, result.Value);
        _apiClients.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("PC1.WEBAGENT", stored.Name);
        Assert.Null(stored.Server);
        Assert.Null(stored.ApiKey);
        Assert.Equal(4, stored.Version);
        _apiClients.Verify(r => r.Add(It.IsAny<ApiClient>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("Pc1.WebAgent", TestData.NewApiClient("Pc1.WebAgent", version: 3));

        Result<int> result = await Handle(TestData.ApiClientModel("Pc1.WebAgent", version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"ApiClient Pc1.WebAgent Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("Pc1.WebAgent", null);

        Result<int> result = await Handle(TestData.ApiClientModel("Pc1.WebAgent", version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ApiClient With Name Pc1.WebAgent Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save, so the concurrency token stopped the UPDATE
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _apiClients.SetupSequence(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewApiClient("Pc1.WebAgent"))
            .ReturnsAsync(TestData.NewApiClient("Pc1.WebAgent", version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.ApiClientModel("Pc1.WebAgent", version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ApiClient Pc1.WebAgent Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        _apiClients.SetupSequence(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiClient?)null).ReturnsAsync(TestData.NewApiClient("Pc1.WebAgent"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle(TestData.ApiClientModel("Pc1.WebAgent"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ApiClient Pc1.WebAgent Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }
}
