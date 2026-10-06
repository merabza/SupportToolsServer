using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.DatabaseServerConnections;

public sealed class DeleteDatabaseServerConnectionCommandHandlerTests
{
    private readonly DatabaseServerConnection _connection =
        TestData.NewDatabaseServerConnection("Pc1.Sql", foldersSetNames: ["Default"], version: 3);

    private readonly Mock<IDatabaseServerConnectionRepository> _connections = new();
    private readonly Server _dl360 = TestData.NewServer("dl360");
    private readonly Mock<IDeploymentEnvironmentRepository> _environments = new();
    private readonly Server _pazisi = TestData.NewServer("PAZISI");
    private readonly DeploymentEnvironment _prod = TestData.NewEnvironment("Prod");
    private readonly Mock<IProjectCreatorSettingsRepository> _projectCreatorSettings = new();
    private readonly List<Project> _projectList = [];
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IServerRepository> _servers = new();
    private readonly DeploymentEnvironment _test = TestData.NewEnvironment("Test");
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public DeleteDatabaseServerConnectionCommandHandlerTests()
    {
        _projects.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => _projectList);
        _servers.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => [_pazisi, _dl360]);
        _environments.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => [_prod, _test]);
    }

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteDatabaseServerConnectionCommandHandler(_connections.Object,
            _projectCreatorSettings.Object, _projects.Object, _servers.Object, _environments.Object,
            _unitOfWork.Object);
        return handler.Handle(new DeleteDatabaseServerConnectionCommand(name, version), cancellationToken);
    }

    //A project is named for its own database parameters, and each of its server infos for theirs: the projects in name
    //order, each before its server infos
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheProjectsAndTheServerInfosThatUseTheConnection()
    {
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);
        DatabaseServerConnection other = TestData.NewDatabaseServerConnection("Pc2.Sql");
        _projectList.AddRange(
            TestData.NewProject("AppC",
                serverInfos: [TestData.NewServerInfo(_pazisi, _prod, null, TestData.NewDatabaseParameters(other))]),
            TestData.NewProject("AppB",
                serverInfos:
                [
                    TestData.NewServerInfo(_dl360, _test, null, null, TestData.NewDatabaseParameters(_connection)),
                    TestData.NewServerInfo(_pazisi, _test)
                ]),
            TestData.NewProject("AppA", devDatabaseParameters: TestData.NewDatabaseParameters(_connection),
                serverInfos:
                [
                    TestData.NewServerInfo(_pazisi, _prod, null, TestData.NewDatabaseParameters(_connection),
                        TestData.NewDatabaseParameters(_connection)),
                    TestData.NewServerInfo(_dl360, _prod, null, TestData.NewDatabaseParameters(_connection))
                ]));
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("Pc1.Sql", 3, cancellation.Token);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(
            "DatabaseServerConnection Pc1.Sql Is Used By: Project AppA, Project AppA / dl360|Prod, " +
            "Project AppA / PAZISI|Prod, Project AppB / dl360|Test", result.Error.Description);
        _servers.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _environments.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        VerifyNothingDeleted();
    }

    //The field of the singleton comes first, then the projects whose dev or prod copy database parameters use the
    //connection, in name order and each once
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheSettingsFieldAndTheProjects()
    {
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings(developerDbConnection: _connection));
        DatabaseServerConnection other = TestData.NewDatabaseServerConnection("Pc2.Sql");
        _projectList.AddRange(
            TestData.NewProject("AppC", prodCopyDatabaseParameters: TestData.NewDatabaseParameters(_connection)),
            TestData.NewProject("AppB", devDatabaseParameters: TestData.NewDatabaseParameters(other),
                prodCopyDatabaseParameters: TestData.NewDatabaseParameters(other)),
            TestData.NewProject("AppA", TestData.NewEditorConfigFileType("default"),
                TestData.NewDatabaseParameters(_connection), TestData.NewDatabaseParameters(_connection)));

        Result result = await Handle("Pc1.Sql", 3);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(
            "DatabaseServerConnection Pc1.Sql Is Used By: ProjectCreatorSettings.DeveloperDbConnectionName, " +
            "Project AppA, Project AppC", result.Error.Description);
        VerifyNothingDeleted();
    }

    private void VerifyNothingDeleted()
    {
        _connections.Verify(r => r.Delete(It.IsAny<DatabaseServerConnection>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _connections.Setup(r => r.GetByName("Pc2.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatabaseServerConnection?)null);

        Result result = await Handle("Pc2.Sql", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("DatabaseServerConnection With Name Pc2.Sql Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    //Project creator settings that name another connection do not use it
    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        _connections.Setup(r => r.GetByName("PC1.SQL", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync(
            TestData.NewProjectCreatorSettings(developerDbConnection: TestData.NewDatabaseServerConnection("Pc2.Sql")));
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("PC1.SQL", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _projectCreatorSettings.Verify(r => r.Get(cancellation.Token), Times.Once);
        _connections.Verify(r => r.Delete(_connection), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //The singleton has no name, so its field names the user
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheFieldOfTheProjectCreatorSettings()
    {
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings(developerDbConnection: _connection));

        Result result = await Handle("Pc1.Sql", 3);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(
            "DatabaseServerConnection Pc1.Sql Is Used By: ProjectCreatorSettings.DeveloperDbConnectionName",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);

        Result result = await Handle("Pc1.Sql", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"DatabaseServerConnection Pc1.Sql Version Conflict: Expected {version}, Actual 3",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);

        Result result = await Handle("Pc1.Sql", null);

        Assert.True(result.IsSuccess);
        _connections.Verify(r => r.Delete(_connection), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _connections.SetupSequence(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_connection).ReturnsAsync(TestData.NewDatabaseServerConnection("Pc1.Sql", version: 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Pc1.Sql", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DatabaseServerConnection Pc1.Sql Version Conflict: Expected 3, Actual 4",
            result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _connections.SetupSequence(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_connection).ReturnsAsync((DatabaseServerConnection?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Pc1.Sql", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
