using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.Settings;
using SupportToolsServer.Application.Settings.GetProjectCreatorSettings;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Settings;

public sealed class ProjectCreatorSettingsQueryHandlerTests
{
    private readonly FileStorage _backups = TestData.NewFileStorage("Backups");
    private readonly DatabaseServerConnection _connection = TestData.NewDatabaseServerConnection("Pazisi");
    private readonly Mock<IDatabaseServerConnectionRepository> _connections = new();
    private readonly Mock<IDeploymentEnvironmentRepository> _environments = new();
    private readonly Mock<IFileStorageRepository> _fileStorages = new();
    private readonly DeploymentEnvironment _prod = TestData.NewEnvironment("Prod");
    private readonly Mock<IProjectCreatorSettingsRepository> _projectCreatorSettings = new();
    private readonly SmartSchema _reduce = TestData.NewSmartSchema("Reduce");
    private readonly Server _server = TestData.NewServer("dl360");
    private readonly Mock<IServerRepository> _servers = new();
    private readonly Mock<ISmartSchemaRepository> _smartSchemas = new();

    public ProjectCreatorSettingsQueryHandlerTests()
    {
        _servers.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewServer("PAZISI"), _server]);
        _environments.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_prod, TestData.NewEnvironment("Dev")]);
        _connections.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_connection]);
        _fileStorages.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewFileStorage("Exchange"), _backups]);
        _smartSchemas.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_reduce]);
    }

    private Task<Result<StsProjectCreatorSettingsDataModel>> Handle(CancellationToken cancellationToken = default)
    {
        return new GetProjectCreatorSettingsQueryHandler(_projectCreatorSettings.Object, _servers.Object,
                _environments.Object, _connections.Object, _fileStorages.Object, _smartSchemas.Object)
            .Handle(new GetProjectCreatorSettingsQuery(), cancellationToken);
    }

    //Before the first create the client reads an empty contract with version 0 instead of an error, and the
    //referenced records are not read
    [Fact]
    public async Task Handle_ReturnsAnEmptyContractWithVersionZero_BeforeTheSingletonIsCreated()
    {
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCreatorSettings?)null);

        Result<StsProjectCreatorSettingsDataModel> result = await Handle();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Version);
        Assert.Equal(0, result.Value.IndentSize);
        Assert.Null(result.Value.FakeHostProjectName);
        Assert.Null(result.Value.ProductionServerName);
        _servers.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _environments.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _connections.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _fileStorages.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _smartSchemas.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
    }

    //The references are named, not given by their ids
    [Fact]
    public async Task Handle_ReturnsTheSingletonWithTheNamesOfItsReferencesAndItsVersion()
    {
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync(
            TestData.NewProjectCreatorSettings(_server, _prod, _connection, _backups, _reduce, 5));
        using var cancellation = new CancellationTokenSource();

        Result<StsProjectCreatorSettingsDataModel> result = await Handle(cancellation.Token);

        StsProjectCreatorSettingsDataModel model = result.Value;
        Assert.Equal("dl360", model.ProductionServerName);
        Assert.Equal("Prod", model.ProductionEnvironmentName);
        Assert.Equal("Pazisi", model.DeveloperDbConnectionName);
        Assert.Equal("Backups", model.DatabaseExchangeFileStorageName);
        Assert.Equal("Reduce", model.UseSmartSchema);
        Assert.Equal(5, model.Version);
        _projectCreatorSettings.Verify(r => r.Get(cancellation.Token), Times.Once);
        _servers.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _environments.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _connections.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _fileStorages.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _smartSchemas.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsNoNames_WhenTheSingletonHasNoReferences()
    {
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings());

        Result<StsProjectCreatorSettingsDataModel> result = await Handle();

        Assert.Null(result.Value.ProductionServerName);
        Assert.Null(result.Value.ProductionEnvironmentName);
        Assert.Null(result.Value.DeveloperDbConnectionName);
        Assert.Null(result.Value.DatabaseExchangeFileStorageName);
        Assert.Null(result.Value.UseSmartSchema);
        Assert.Equal(1, result.Value.Version);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        ProjectCreatorSettings projectCreatorSettings =
            TestData.NewProjectCreatorSettings(_server, _prod, _connection, _backups, _reduce, 7);

        StsProjectCreatorSettingsDataModel model = projectCreatorSettings.ToContractModel(
            new Dictionary<ServerId, string> { [_server.Id] = "dl360" },
            new Dictionary<DeploymentEnvironmentId, string> { [_prod.Id] = "Prod" },
            new Dictionary<DatabaseServerConnectionId, string> { [_connection.Id] = "Pazisi" },
            new Dictionary<FileStorageId, string> { [_backups.Id] = "Backups" },
            new Dictionary<SmartSchemaId, string> { [_reduce.Id] = "Reduce" });

        Assert.Equal(4, model.IndentSize);
        Assert.Equal("FakeHost", model.FakeHostProjectName);
        Assert.Equal(@"D:\1WorkDotnet", model.ProjectsFolderPathReal);
        Assert.Equal(@"D:\1WorkSecurity", model.SecretsFolderPathReal);
        Assert.Equal("dl360", model.ProductionServerName);
        Assert.Equal("Prod", model.ProductionEnvironmentName);
        Assert.Equal("Pazisi", model.DeveloperDbConnectionName);
        Assert.Equal("Backups", model.DatabaseExchangeFileStorageName);
        Assert.Equal("Reduce", model.UseSmartSchema);
        Assert.Equal(7, model.Version);
    }

    //Before the singleton is created nothing uses a record
    [Fact]
    public void GetUsages_IsEmpty_BeforeTheSingletonIsCreated()
    {
        ProjectCreatorSettings? projectCreatorSettings = null;

        Assert.Empty(projectCreatorSettings.GetUsages(_server.Id));
        Assert.Empty(projectCreatorSettings.GetUsages(_prod.Id));
        Assert.Empty(projectCreatorSettings.GetUsages(_connection.Id));
        Assert.Empty(projectCreatorSettings.GetUsages(_backups.Id));
        Assert.Empty(projectCreatorSettings.GetUsages(_reduce.Id));
    }
}
