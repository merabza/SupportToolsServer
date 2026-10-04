using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.ApiClients.DeleteApiClient;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.Servers;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ApiClients;

public sealed class DeleteApiClientCommandHandlerTests
{
    private readonly ApiClient _apiClient = TestData.NewApiClient("Pc1.WebAgent", version: 3);
    private readonly Mock<IApiClientRepository> _apiClients = new();
    private readonly Mock<IDatabaseServerConnectionRepository> _connections = new();
    private readonly Mock<IServerRepository> _servers = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public DeleteApiClientCommandHandlerTests()
    {
        _connections.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _servers.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteApiClientCommandHandler(_apiClients.Object, _connections.Object, _servers.Object,
            _unitOfWork.Object);
        return handler.Handle(new DeleteApiClientCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _apiClients.Verify(r => r.Delete(It.IsAny<ApiClient>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _apiClients.Setup(r => r.GetByName("Pc2.WebAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiClient?)null);

        Result result = await Handle("Pc2.WebAgent", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ApiClient With Name Pc2.WebAgent Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    //A connection or a server without a web agent and those with another web agent do not use it
    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStoredAndNoConnectionOrServerUsesIt()
    {
        ApiClient other = TestData.NewApiClient("Pc2.WebAgent");
        _apiClients.Setup(r => r.GetByName("PC1.WEBAGENT", It.IsAny<CancellationToken>())).ReturnsAsync(_apiClient);
        _connections.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewDatabaseServerConnection("Pc1.Sql"), TestData.NewDatabaseServerConnection("Pc2.Sql", other)
        ]);
        _servers.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewServer("PAZISI"), TestData.NewServer("dl360", other, other)]);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("PC1.WEBAGENT", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _apiClients.Verify(r => r.Delete(_apiClient), Times.Once);
        _connections.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _servers.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //A server uses the ApiClient as its web agent, as the installer of the applications or as both, and is named once
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheServersThatUseIt()
    {
        ApiClient other = TestData.NewApiClient("Pc2.WebAgent");
        _apiClients.Setup(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>())).ReturnsAsync(_apiClient);
        _servers.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewServer("PAZISI", _apiClient, other), TestData.NewServer("dl360", other, other),
            TestData.NewServer("bee", other, _apiClient), TestData.NewServer("guria", _apiClient, _apiClient)
        ]);

        Result result = await Handle("Pc1.WebAgent", 3);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal("ApiClient Pc1.WebAgent Is Used By: Server bee, Server guria, Server PAZISI",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    //The connections come first, then the servers, each in name order
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheConnectionsAndTheServersThatUseIt()
    {
        _apiClients.Setup(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>())).ReturnsAsync(_apiClient);
        _connections.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewDatabaseServerConnection("Pc1.Sql", _apiClient)]);
        _servers.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewServer("PAZISI", _apiClient), TestData.NewServer("archive", _apiClient)]);

        Result result = await Handle("Pc1.WebAgent", 3);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(
            "ApiClient Pc1.WebAgent Is Used By: DatabaseServerConnection Pc1.Sql, Server archive, Server PAZISI",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    //The users are named by their type and name, in name order
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheConnectionsThatUseIt()
    {
        _apiClients.Setup(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>())).ReturnsAsync(_apiClient);
        _connections.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewDatabaseServerConnection("Pc1.Sql", _apiClient),
            TestData.NewDatabaseServerConnection("Other.Sql", TestData.NewApiClient("Pc2.WebAgent")),
            TestData.NewDatabaseServerConnection("archive.Sql", _apiClient)
        ]);

        Result result = await Handle("Pc1.WebAgent", 3);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(
            "ApiClient Pc1.WebAgent Is Used By: DatabaseServerConnection archive.Sql, DatabaseServerConnection Pc1.Sql",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _apiClients.Setup(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>())).ReturnsAsync(_apiClient);

        Result result = await Handle("Pc1.WebAgent", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"ApiClient Pc1.WebAgent Version Conflict: Expected {version}, Actual 3",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _apiClients.Setup(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>())).ReturnsAsync(_apiClient);

        Result result = await Handle("Pc1.WebAgent", null);

        Assert.True(result.IsSuccess);
        _apiClients.Verify(r => r.Delete(_apiClient), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _apiClients.SetupSequence(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_apiClient).ReturnsAsync(TestData.NewApiClient("Pc1.WebAgent", version: 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Pc1.WebAgent", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("ApiClient Pc1.WebAgent Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _apiClients.SetupSequence(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_apiClient).ReturnsAsync((ApiClient?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Pc1.WebAgent", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
