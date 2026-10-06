using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Environments.DeleteEnvironment;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Environments;

public sealed class DeleteEnvironmentCommandHandlerTests
{
    private readonly Server _dl360 = TestData.NewServer("dl360");
    private readonly Mock<IDeploymentEnvironmentRepository> _environments = new();
    private readonly Server _pazisi = TestData.NewServer("PAZISI");
    private readonly DeploymentEnvironment _prod = TestData.NewEnvironment("Prod", "Production", 3);
    private readonly Mock<IProjectCreatorSettingsRepository> _projectCreatorSettings = new();
    private readonly List<Project> _projectList = [];
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IServerRepository> _servers = new();
    private readonly DeploymentEnvironment _test = TestData.NewEnvironment("Test");
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public DeleteEnvironmentCommandHandlerTests()
    {
        _projects.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => _projectList);
        _servers.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => [_pazisi, _dl360]);
        _environments.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => [_test, _prod]);
    }

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteEnvironmentCommandHandler(_environments.Object, _projectCreatorSettings.Object,
            _projects.Object, _servers.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteEnvironmentCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _environments.Verify(r => r.Delete(It.IsAny<DeploymentEnvironment>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _environments.Setup(r => r.GetByName("Stage", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeploymentEnvironment?)null);

        Result result = await Handle("Stage", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Environment With Name Stage Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    //Project creator settings and server infos that name another environment do not use it
    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        _environments.Setup(r => r.GetByName("prod", It.IsAny<CancellationToken>())).ReturnsAsync(_prod);
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync(
            TestData.NewProjectCreatorSettings(productionEnvironment: TestData.NewEnvironment("Stage")));
        _projectList.Add(TestData.NewProject("AppA", serverInfos: [TestData.NewServerInfo(_pazisi, _test)]));
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("prod", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _projectCreatorSettings.Verify(r => r.Get(cancellation.Token), Times.Once);
        _projects.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _environments.Verify(r => r.Delete(_prod), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //A server info is named by its project, its server and its environment: the projects in name order, the server
    //infos of a project by the server and the environment, after the field of the project creator settings
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheServerInfosThatUseTheEnvironment()
    {
        _environments.Setup(r => r.GetByName("Prod", It.IsAny<CancellationToken>())).ReturnsAsync(_prod);
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings(productionEnvironment: _prod));
        _projectList.AddRange(
            TestData.NewProject("AppB",
                serverInfos: [TestData.NewServerInfo(_pazisi, _prod), TestData.NewServerInfo(_dl360, _prod)]),
            TestData.NewProject("AppC", serverInfos: [TestData.NewServerInfo(_pazisi, _test)]),
            TestData.NewProject("appA",
                serverInfos: [TestData.NewServerInfo(_dl360, _test), TestData.NewServerInfo(_dl360, _prod)]));
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("Prod", 3, cancellation.Token);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(
            "Environment Prod Is Used By: ProjectCreatorSettings.ProductionEnvironmentName, " +
            "Project appA / dl360|Prod, Project AppB / dl360|Prod, Project AppB / PAZISI|Prod",
            result.Error.Description);
        _servers.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _environments.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        VerifyNothingDeleted();
    }

    //The singleton has no name, so its field names the user
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheFieldOfTheProjectCreatorSettings()
    {
        _environments.Setup(r => r.GetByName("Prod", It.IsAny<CancellationToken>())).ReturnsAsync(_prod);
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings(productionEnvironment: _prod));

        Result result = await Handle("Prod", 3);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Environment Prod Is Used By: ProjectCreatorSettings.ProductionEnvironmentName",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _environments.Setup(r => r.GetByName("Prod", It.IsAny<CancellationToken>())).ReturnsAsync(_prod);

        Result result = await Handle("Prod", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"Environment Prod Version Conflict: Expected {version}, Actual 3", result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _environments.Setup(r => r.GetByName("Prod", It.IsAny<CancellationToken>())).ReturnsAsync(_prod);

        Result result = await Handle("Prod", null);

        Assert.True(result.IsSuccess);
        _environments.Verify(r => r.Delete(_prod), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _environments.SetupSequence(r => r.GetByName("Prod", It.IsAny<CancellationToken>())).ReturnsAsync(_prod)
            .ReturnsAsync(TestData.NewEnvironment("Prod", "Changed", 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Prod", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Environment Prod Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _environments.SetupSequence(r => r.GetByName("Prod", It.IsAny<CancellationToken>())).ReturnsAsync(_prod)
            .ReturnsAsync((DeploymentEnvironment?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Prod", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
