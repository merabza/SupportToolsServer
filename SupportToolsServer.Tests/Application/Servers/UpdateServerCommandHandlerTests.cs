using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Servers.UpdateServer;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Servers;

public sealed class UpdateServerCommandHandlerTests
{
    private readonly Mock<IApiClientRepository> _apiClients = new();
    private readonly Runtime _runtime = TestData.NewRuntime("linux-x64");
    private readonly Mock<IRuntimeRepository> _runtimes = new();
    private readonly Mock<IServerRepository> _servers = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ApiClient _webAgent = TestData.NewApiClient("Dl360.WebAgent");
    private readonly ApiClient _webAgentInstaller = TestData.NewApiClient("Dl360.Installer");

    public UpdateServerCommandHandlerTests()
    {
        _apiClients.Setup(r => r.GetByName("Dl360.WebAgent", It.IsAny<CancellationToken>())).ReturnsAsync(_webAgent);
        _apiClients.Setup(r => r.GetByName("Dl360.Installer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_webAgentInstaller);
        _apiClients.Setup(r => r.GetByName("Pc9.WebAgent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiClient?)null);
        _apiClients.Setup(r => r.GetByName("Pc9.Installer", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiClient?)null);
        _runtimes.Setup(r => r.GetByName("linux-x64", It.IsAny<CancellationToken>())).ReturnsAsync(_runtime);
        _runtimes.Setup(r => r.GetByName("osx-arm64", It.IsAny<CancellationToken>())).ReturnsAsync((Runtime?)null);
    }

    private void GivenStored(string name, Server? stored)
    {
        _servers.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsServerDataModel model, CancellationToken cancellationToken = default)
    {
        var handler = new UpdateServerCommandHandler(_servers.Object, _apiClients.Object, _runtimes.Object,
            _unitOfWork.Object);
        return handler.Handle(new UpdateServerCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _servers.Verify(r => r.Add(It.IsAny<Server>()), Times.Never);
        _servers.Verify(r => r.Update(It.IsAny<Server>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithItsReferencesAndTheFirstVersion()
    {
        GivenStored("dl360", null);
        Server? added = null;
        _servers.Setup(r => r.Add(It.IsAny<Server>())).Callback<Server>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result =
            await Handle(TestData.ServerModel("dl360", "Dl360.WebAgent", "Dl360.Installer", "linux-x64"),
                cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("dl360", added.Name);
        Assert.Equal(_webAgent.Id, added.WebAgentId);
        Assert.Equal(_webAgentInstaller.Id, added.WebAgentInstallerId);
        Assert.Equal("deployer", added.FilesUserName);
        Assert.Equal("deployers", added.FilesUsersGroupName);
        Assert.Equal(_runtime.Id, added.RuntimeId);
        Assert.Equal("/home/deployer/Download", added.ServerSideDownloadFolder);
        Assert.Equal("/opt/apps", added.ServerSideDeployFolder);
        Assert.Equal(1, added.Version);
        _apiClients.Verify(r => r.GetByName("Dl360.WebAgent", cancellation.Token), Times.Once);
        _apiClients.Verify(r => r.GetByName("Dl360.Installer", cancellation.Token), Times.Once);
        _runtimes.Verify(r => r.GetByName("linux-x64", cancellation.Token), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //No name is no reference: the referenced records are not even read
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Handle_StoresNoReferences_WhenTheBodyNamesNone(string? name)
    {
        GivenStored("dl360", null);
        Server? added = null;
        _servers.Setup(r => r.Add(It.IsAny<Server>())).Callback<Server>(x => added = x);

        Result<int> result = await Handle(TestData.ServerModel("dl360", name, name, name));

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Null(added.WebAgentId);
        Assert.Null(added.WebAgentInstallerId);
        Assert.Null(added.RuntimeId);
        _apiClients.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _runtimes.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    //One web agent may install and serve the applications
    [Fact]
    public async Task Handle_AcceptsTheSameApiClientForBothWebAgents()
    {
        GivenStored("dl360", null);
        Server? added = null;
        _servers.Setup(r => r.Add(It.IsAny<Server>())).Callback<Server>(x => added = x);

        Result<int> result = await Handle(TestData.ServerModel("dl360", "Dl360.WebAgent", "Dl360.WebAgent"));

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal(_webAgent.Id, added.WebAgentId);
        Assert.Equal(_webAgent.Id, added.WebAgentInstallerId);
    }

    //Every missing name comes at once, grouped by the type of the referenced record
    [Fact]
    public async Task Handle_ReturnsReferencedRecordsNotFoundWithEveryMissingName()
    {
        GivenStored("dl360", TestData.NewServer("dl360", version: 2));

        Result<int> result =
            await Handle(TestData.ServerModel("dl360", "Pc9.WebAgent", "Pc9.Installer", "osx-arm64", 2));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Referenced ApiClient Records Not Found: Pc9.WebAgent, Pc9.Installer; " +
                     "Referenced Runtime Records Not Found: osx-arm64", result.Error.Description);
        VerifyNothingSaved();
    }

    //A missing web agent named in both fields is named once
    [Theory]
    [InlineData("Pc9.WebAgent", "Dl360.Installer", "linux-x64",
        "Referenced ApiClient Records Not Found: Pc9.WebAgent")]
    [InlineData("Dl360.WebAgent", "Pc9.Installer", "linux-x64",
        "Referenced ApiClient Records Not Found: Pc9.Installer")]
    [InlineData("Pc9.WebAgent", "Pc9.WebAgent", null, "Referenced ApiClient Records Not Found: Pc9.WebAgent")]
    [InlineData("Dl360.WebAgent", "Dl360.Installer", "osx-arm64",
        "Referenced Runtime Records Not Found: osx-arm64")]
    [InlineData(null, null, "osx-arm64", "Referenced Runtime Records Not Found: osx-arm64")]
    public async Task Handle_ReturnsReferencedRecordsNotFound_NamingOnlyTheMissingRecords(string? webAgentName,
        string? webAgentInstallerName, string? runtime, string description)
    {
        GivenStored("dl360", null);

        Result<int> result =
            await Handle(TestData.ServerModel("dl360", webAgentName, webAgentInstallerName, runtime));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal(description, result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("dl360", TestData.NewServer("DL360", version: 2));

        Result<int> result = await Handle(TestData.ServerModel("dl360"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Server dl360 Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The version is checked first: a stale request reads no referenced record, even a missing one
    [Fact]
    public async Task Handle_ChecksTheVersionBeforeTheReferences()
    {
        GivenStored("dl360", TestData.NewServer("dl360", version: 3));

        Result<int> result = await Handle(TestData.ServerModel("dl360", "Pc9.WebAgent", null, "osx-arm64", 2));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        _apiClients.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _runtimes.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved();
    }

    //The route key gives the name, so changing its case is an update; the references may be dropped
    [Fact]
    public async Task Handle_UpdatesTheStoredRecordAndReturnsItsNextVersion()
    {
        Server stored = TestData.NewServer("dl360", _webAgent, _webAgentInstaller, _runtime, 3);
        GivenStored("DL360", stored);
        StsServerDataModel model = TestData.ServerModel("DL360", "Dl360.Installer", null, null, 3);
        model.FilesUserName = "admin";
        model.ServerSideDeployFolder = null;

        Result<int> result = await Handle(model);

        Assert.Equal(4, result.Value);
        _servers.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("DL360", stored.Name);
        Assert.Equal(_webAgentInstaller.Id, stored.WebAgentId);
        Assert.Null(stored.WebAgentInstallerId);
        Assert.Null(stored.RuntimeId);
        Assert.Equal("admin", stored.FilesUserName);
        Assert.Equal("deployers", stored.FilesUsersGroupName);
        Assert.Equal("/home/deployer/Download", stored.ServerSideDownloadFolder);
        Assert.Null(stored.ServerSideDeployFolder);
        Assert.Equal(4, stored.Version);
        _servers.Verify(r => r.Add(It.IsAny<Server>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("dl360", TestData.NewServer("dl360", version: 3));

        Result<int> result = await Handle(TestData.ServerModel("dl360", version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"Server dl360 Version Conflict: Expected {expectedVersion}, Actual 3", result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("dl360", null);

        Result<int> result = await Handle(TestData.ServerModel("dl360", version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Server With Name dl360 Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _servers.SetupSequence(r => r.GetByName("dl360", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewServer("dl360")).ReturnsAsync(TestData.NewServer("dl360", version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.ServerModel("dl360", version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Server dl360 Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        _servers.SetupSequence(r => r.GetByName("dl360", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Server?)null).ReturnsAsync(TestData.NewServer("dl360"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle(TestData.ServerModel("dl360"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Server dl360 Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }

    //A save that fails for another reason (here: a referenced record was deleted after it was read) keeps its
    //exception. The second read is another instance, as from the database: the handler incremented the version of the
    //first one
    [Fact]
    public async Task Handle_ThrowsTheSaveException_WhenTheVersionStillMatches()
    {
        _servers.SetupSequence(r => r.GetByName("dl360", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewServer("dl360")).ReturnsAsync(TestData.NewServer("dl360"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("foreign key"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Handle(TestData.ServerModel("dl360", "Dl360.WebAgent", null, "linux-x64", 1)));
    }
}
