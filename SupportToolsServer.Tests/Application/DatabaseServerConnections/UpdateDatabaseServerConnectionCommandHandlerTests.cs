using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.DatabaseServerConnections.UpdateDatabaseServerConnection;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.DatabaseServerConnections;

public sealed class UpdateDatabaseServerConnectionCommandHandlerTests
{
    private readonly Mock<IApiClientRepository> _apiClients = new();
    private readonly Mock<IDatabaseServerConnectionRepository> _connections = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ApiClient _webAgent = TestData.NewApiClient("Pc1.WebAgent");

    public UpdateDatabaseServerConnectionCommandHandlerTests()
    {
        _apiClients.Setup(r => r.GetByName("Pc1.WebAgent", It.IsAny<CancellationToken>())).ReturnsAsync(_webAgent);
    }

    //The handler reads the connection with tracking, so that the save deletes the replaced folders sets
    private void GivenStored(string name, DatabaseServerConnection? stored)
    {
        _connections.Setup(r => r.GetByNameForUpdate(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsDatabaseServerConnectionDataModel model,
        CancellationToken cancellationToken = default)
    {
        var handler =
            new UpdateDatabaseServerConnectionCommandHandler(_connections.Object, _apiClients.Object,
                _unitOfWork.Object);
        return handler.Handle(new UpdateDatabaseServerConnectionCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _connections.Verify(r => r.Add(It.IsAny<DatabaseServerConnection>()), Times.Never);
        _connections.Verify(r => r.Update(It.IsAny<DatabaseServerConnection>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithTheWebAgentAndFoldersSetsAndTheFirstVersion()
    {
        GivenStored("Pc1.Sql", null);
        DatabaseServerConnection? added = null;
        _connections.Setup(r => r.Add(It.IsAny<DatabaseServerConnection>()))
            .Callback<DatabaseServerConnection>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result =
            await Handle(TestData.DatabaseServerConnectionModel("Pc1.Sql", "Pc1.WebAgent", ["Default", "Second"]),
                cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("Pc1.Sql", added.Name);
        Assert.Equal("SqlServer", added.DatabaseServerProvider);
        Assert.Equal(_webAgent.Id, added.DbWebAgentId);
        Assert.Equal("Main", added.RemoteDbConnectionName);
        Assert.Equal("pc1", added.ServerAddress);
        Assert.True(added.WindowsNtIntegratedSecurity);
        Assert.Equal(TestData.MadeUpUser, added.ServerUser);
        Assert.Equal(TestData.MadeUpPassword, added.ServerPass);
        Assert.True(added.TrustServerCertificate);
        Assert.Equal(30, added.ConnectionTimeOut);
        Assert.True(added.Encrypt);
        Assert.Equal(["Default", "Second"], added.DatabaseFoldersSets.Select(x => x.Name));
        Assert.Equal(@"D:\Default\Bak", added.DatabaseFoldersSets[0].Backup);
        Assert.Equal(@"D:\Default\Data", added.DatabaseFoldersSets[0].Data);
        Assert.Equal(@"D:\Default\Log", added.DatabaseFoldersSets[0].DataLog);
        Assert.Equal(1, added.Version);
        _apiClients.Verify(r => r.GetByName("Pc1.WebAgent", cancellation.Token), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //No web agent is no reference: the API clients are not even read
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Handle_StoresNoWebAgent_WhenTheBodyNamesNone(string? dbWebAgentName)
    {
        GivenStored("Pc1.Sql", null);
        DatabaseServerConnection? added = null;
        _connections.Setup(r => r.Add(It.IsAny<DatabaseServerConnection>()))
            .Callback<DatabaseServerConnection>(x => added = x);

        Result<int> result = await Handle(TestData.DatabaseServerConnectionModel("Pc1.Sql", dbWebAgentName));

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Null(added.DbWebAgentId);
        _apiClients.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsReferencedRecordsNotFound_WhenTheWebAgentDoesNotExist()
    {
        GivenStored("Pc1.Sql", TestData.NewDatabaseServerConnection("Pc1.Sql", version: 2));
        _apiClients.Setup(r => r.GetByName("Pc2.WebAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiClient?)null);

        Result<int> result =
            await Handle(TestData.DatabaseServerConnectionModel("Pc1.Sql", "Pc2.WebAgent", version: 2));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Referenced ApiClient Records Not Found: Pc2.WebAgent", result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("Pc1.Sql", TestData.NewDatabaseServerConnection("PC1.SQL", version: 2));

        Result<int> result = await Handle(TestData.DatabaseServerConnectionModel("Pc1.Sql"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("DatabaseServerConnection Pc1.Sql Version Conflict: Expected 0, Actual 2",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The update replaces the whole aggregate: the stored folders sets are gone, those of the body are the only ones
    [Fact]
    public async Task Handle_ReplacesTheStoredRecordWithItsFoldersSetsAndReturnsItsNextVersion()
    {
        DatabaseServerConnection stored =
            TestData.NewDatabaseServerConnection("Pc1.Sql", _webAgent, ["Default", "Old"], 3);
        GivenStored("PC1.SQL", stored);
        StsDatabaseServerConnectionDataModel model =
            TestData.DatabaseServerConnectionModel("PC1.SQL", null, ["Default", "New"], 3);
        model.DatabaseServerProvider = "WebAgent";
        model.ServerPass = null;

        Result<int> result = await Handle(model);

        Assert.Equal(4, result.Value);
        _connections.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("PC1.SQL", stored.Name);
        Assert.Equal("WebAgent", stored.DatabaseServerProvider);
        Assert.Null(stored.DbWebAgentId);
        Assert.Null(stored.ServerPass);
        Assert.Equal(["Default", "New"], stored.DatabaseFoldersSets.Select(x => x.Name));
        Assert.Equal(4, stored.Version);
        _connections.Verify(r => r.Add(It.IsAny<DatabaseServerConnection>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("Pc1.Sql", TestData.NewDatabaseServerConnection("Pc1.Sql", version: 3));

        Result<int> result =
            await Handle(TestData.DatabaseServerConnectionModel("Pc1.Sql", version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"DatabaseServerConnection Pc1.Sql Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("Pc1.Sql", null);

        Result<int> result = await Handle(TestData.DatabaseServerConnectionModel("Pc1.Sql", version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("DatabaseServerConnection With Name Pc1.Sql Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save. The version is read again without tracking, so it
    //is the stored one and not the version of the connection this request changed
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        GivenStored("Pc1.Sql", TestData.NewDatabaseServerConnection("Pc1.Sql"));
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewDatabaseServerConnection("Pc1.Sql", version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.DatabaseServerConnectionModel("Pc1.Sql", version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DatabaseServerConnection Pc1.Sql Version Conflict: Expected 1, Actual 2",
            result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        GivenStored("Pc1.Sql", null);
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewDatabaseServerConnection("Pc1.Sql"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle(TestData.DatabaseServerConnectionModel("Pc1.Sql"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DatabaseServerConnection Pc1.Sql Version Conflict: Expected 0, Actual 1",
            result.Error.Description);
    }

    //A save that fails for another reason (here: the web agent was deleted after it was read) keeps its exception
    [Fact]
    public async Task Handle_ThrowsTheSaveException_WhenTheVersionStillMatches()
    {
        GivenStored("Pc1.Sql", TestData.NewDatabaseServerConnection("Pc1.Sql"));
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewDatabaseServerConnection("Pc1.Sql"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("foreign key"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Handle(TestData.DatabaseServerConnectionModel("Pc1.Sql", "Pc1.WebAgent", version: 1)));
    }
}
