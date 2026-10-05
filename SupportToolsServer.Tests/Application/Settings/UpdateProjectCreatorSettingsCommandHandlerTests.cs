using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Settings.UpdateProjectCreatorSettings;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Settings;

public sealed class UpdateProjectCreatorSettingsCommandHandlerTests
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
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateProjectCreatorSettingsCommandHandlerTests()
    {
        _servers.Setup(r => r.GetByName("dl360", It.IsAny<CancellationToken>())).ReturnsAsync(_server);
        _servers.Setup(r => r.GetByName("guria", It.IsAny<CancellationToken>())).ReturnsAsync((Server?)null);
        _environments.Setup(r => r.GetByName("Prod", It.IsAny<CancellationToken>())).ReturnsAsync(_prod);
        _environments.Setup(r => r.GetByName("Stage", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeploymentEnvironment?)null);
        _connections.Setup(r => r.GetByName("Pazisi", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);
        _connections.Setup(r => r.GetByName("Pc9.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatabaseServerConnection?)null);
        _fileStorages.Setup(r => r.GetByName("Backups", It.IsAny<CancellationToken>())).ReturnsAsync(_backups);
        _fileStorages.Setup(r => r.GetByName("Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileStorage?)null);
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>())).ReturnsAsync(_reduce);
        _smartSchemas.Setup(r => r.GetByName("Hourly", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartSchema?)null);
    }

    private void GivenStored(ProjectCreatorSettings? stored)
    {
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsProjectCreatorSettingsDataModel model,
        CancellationToken cancellationToken = default)
    {
        var handler = new UpdateProjectCreatorSettingsCommandHandler(_projectCreatorSettings.Object, _servers.Object,
            _environments.Object, _connections.Object, _fileStorages.Object, _smartSchemas.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateProjectCreatorSettingsCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _projectCreatorSettings.Verify(r => r.Add(It.IsAny<ProjectCreatorSettings>()), Times.Never);
        _projectCreatorSettings.Verify(r => r.Update(It.IsAny<ProjectCreatorSettings>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    //The first create: version 0 is expected, the singleton gets its fixed key and the first version
    [Fact]
    public async Task Handle_CreatesTheSingletonWithItsReferencesAndTheFirstVersion()
    {
        GivenStored(null);
        ProjectCreatorSettings? added = null;
        _projectCreatorSettings.Setup(r => r.Add(It.IsAny<ProjectCreatorSettings>()))
            .Callback<ProjectCreatorSettings>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle(
            TestData.ProjectCreatorSettingsModel("dl360", "Prod", "Pazisi", "Backups", "Reduce"), cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal(ProjectCreatorSettingsId.Singleton, added.Id);
        Assert.Equal(4, added.IndentSize);
        Assert.Equal("FakeHost", added.FakeHostProjectName);
        Assert.Equal(@"D:\1WorkDotnet", added.ProjectsFolderPathReal);
        Assert.Equal(@"D:\1WorkSecurity", added.SecretsFolderPathReal);
        Assert.Equal(_server.Id, added.ProductionServerId);
        Assert.Equal(_prod.Id, added.ProductionEnvironmentId);
        Assert.Equal(_connection.Id, added.DeveloperDbConnectionId);
        Assert.Equal(_backups.Id, added.DatabaseExchangeFileStorageId);
        Assert.Equal(_reduce.Id, added.UseSmartSchemaId);
        Assert.Equal(1, added.Version);
        _projectCreatorSettings.Verify(r => r.Get(cancellation.Token), Times.Once);
        _servers.Verify(r => r.GetByName("dl360", cancellation.Token), Times.Once);
        _environments.Verify(r => r.GetByName("Prod", cancellation.Token), Times.Once);
        _connections.Verify(r => r.GetByName("Pazisi", cancellation.Token), Times.Once);
        _fileStorages.Verify(r => r.GetByName("Backups", cancellation.Token), Times.Once);
        _smartSchemas.Verify(r => r.GetByName("Reduce", cancellation.Token), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //No name is no reference: the referenced records are not even read
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Handle_StoresNoReferences_WhenTheBodyNamesNone(string? name)
    {
        GivenStored(null);
        ProjectCreatorSettings? added = null;
        _projectCreatorSettings.Setup(r => r.Add(It.IsAny<ProjectCreatorSettings>()))
            .Callback<ProjectCreatorSettings>(x => added = x);

        Result<int> result = await Handle(TestData.ProjectCreatorSettingsModel(name, name, name, name, name));

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Null(added.ProductionServerId);
        Assert.Null(added.ProductionEnvironmentId);
        Assert.Null(added.DeveloperDbConnectionId);
        Assert.Null(added.DatabaseExchangeFileStorageId);
        Assert.Null(added.UseSmartSchemaId);
        _servers.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _environments.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _connections.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _fileStorages.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _smartSchemas.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    //Every missing name comes at once, grouped by the type of the referenced record in the order of the contract
    [Fact]
    public async Task Handle_ReturnsReferencedRecordsNotFoundWithEveryMissingName()
    {
        GivenStored(TestData.NewProjectCreatorSettings(version: 2));

        Result<int> result = await Handle(
            TestData.ProjectCreatorSettingsModel("guria", "Stage", "Pc9.Sql", "Missing", "Hourly", 2));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Referenced Server Records Not Found: guria; " +
                     "Referenced Environment Records Not Found: Stage; " +
                     "Referenced DatabaseServerConnection Records Not Found: Pc9.Sql; " +
                     "Referenced FileStorage Records Not Found: Missing; " +
                     "Referenced SmartSchema Records Not Found: Hourly", result.Error.Description);
        VerifyNothingSaved();
    }

    [Theory]
    [InlineData("guria", "Prod", "Referenced Server Records Not Found: guria")]
    [InlineData("dl360", "Stage", "Referenced Environment Records Not Found: Stage")]
    [InlineData(null, "Stage", "Referenced Environment Records Not Found: Stage")]
    public async Task Handle_ReturnsReferencedRecordsNotFound_NamingOnlyTheMissingRecords(string? serverName,
        string? environmentName, string description)
    {
        GivenStored(null);

        Result<int> result = await Handle(TestData.ProjectCreatorSettingsModel(serverName, environmentName));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal(description, result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheSingletonExists()
    {
        GivenStored(TestData.NewProjectCreatorSettings(version: 2));

        Result<int> result = await Handle(TestData.ProjectCreatorSettingsModel());

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Settings ProjectCreator Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The version is checked first: a stale request reads no referenced record, even a missing one
    [Fact]
    public async Task Handle_ChecksTheVersionBeforeTheReferences()
    {
        GivenStored(TestData.NewProjectCreatorSettings(version: 3));

        Result<int> result = await Handle(TestData.ProjectCreatorSettingsModel("guria", version: 2));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        _servers.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_UpdatesTheStoredSingletonAndReturnsItsNextVersion()
    {
        ProjectCreatorSettings stored =
            TestData.NewProjectCreatorSettings(_server, _prod, _connection, _backups, _reduce, 3);
        GivenStored(stored);
        StsProjectCreatorSettingsDataModel model =
            TestData.ProjectCreatorSettingsModel(databaseExchangeFileStorageName: "Backups", version: 3);
        model.IndentSize = 2;
        model.SecretsFolderPathReal = null;

        Result<int> result = await Handle(model);

        Assert.Equal(4, result.Value);
        _projectCreatorSettings.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal(ProjectCreatorSettingsId.Singleton, stored.Id);
        Assert.Equal(2, stored.IndentSize);
        Assert.Equal(@"D:\1WorkDotnet", stored.ProjectsFolderPathReal);
        Assert.Null(stored.SecretsFolderPathReal);
        Assert.Null(stored.ProductionServerId);
        Assert.Null(stored.ProductionEnvironmentId);
        Assert.Null(stored.DeveloperDbConnectionId);
        Assert.Equal(_backups.Id, stored.DatabaseExchangeFileStorageId);
        Assert.Null(stored.UseSmartSchemaId);
        Assert.Equal(4, stored.Version);
        _projectCreatorSettings.Verify(r => r.Add(It.IsAny<ProjectCreatorSettings>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored(TestData.NewProjectCreatorSettings(version: 3));

        Result<int> result = await Handle(TestData.ProjectCreatorSettingsModel(version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"Settings ProjectCreator Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoSingleton()
    {
        GivenStored(null);

        Result<int> result = await Handle(TestData.ProjectCreatorSettingsModel(version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Settings With Name ProjectCreator Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSingletonChangesBeforeTheSave()
    {
        _projectCreatorSettings.SetupSequence(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings())
            .ReturnsAsync(TestData.NewProjectCreatorSettings(version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.ProjectCreatorSettingsModel(version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Settings ProjectCreator Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    //Two first creates at once: the fixed key stops the second INSERT, which reads the stored version again
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSingletonIsCreatedBeforeTheSave()
    {
        _projectCreatorSettings.SetupSequence(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCreatorSettings?)null).ReturnsAsync(TestData.NewProjectCreatorSettings());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("primary key"));

        Result<int> result = await Handle(TestData.ProjectCreatorSettingsModel());

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Settings ProjectCreator Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }

    //A save that fails for another reason keeps its exception. The second read is another instance, as from the
    //database: the handler incremented the version of the first one
    [Fact]
    public async Task Handle_ThrowsTheSaveException_WhenTheVersionStillMatches()
    {
        _projectCreatorSettings.SetupSequence(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings()).ReturnsAsync(TestData.NewProjectCreatorSettings());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("foreign key"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Handle(TestData.ProjectCreatorSettingsModel("dl360", version: 1)));
    }
}
