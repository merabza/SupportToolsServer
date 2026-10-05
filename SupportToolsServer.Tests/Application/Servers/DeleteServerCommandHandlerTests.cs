using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Servers.DeleteServer;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Servers;

public sealed class DeleteServerCommandHandlerTests
{
    private readonly Mock<IProjectCreatorSettingsRepository> _projectCreatorSettings = new();
    private readonly Server _server = TestData.NewServer("dl360", version: 3);
    private readonly Mock<IServerRepository> _servers = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteServerCommandHandler(_servers.Object, _projectCreatorSettings.Object,
            _unitOfWork.Object);
        return handler.Handle(new DeleteServerCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _servers.Verify(r => r.Delete(It.IsAny<Server>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _servers.Setup(r => r.GetByName("guria", It.IsAny<CancellationToken>())).ReturnsAsync((Server?)null);

        Result result = await Handle("guria", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Server With Name guria Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    //Project creator settings that name another server do not use it
    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        _servers.Setup(r => r.GetByName("DL360", It.IsAny<CancellationToken>())).ReturnsAsync(_server);
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings(TestData.NewServer("PAZISI")));
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("DL360", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _projectCreatorSettings.Verify(r => r.Get(cancellation.Token), Times.Once);
        _servers.Verify(r => r.Delete(_server), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //The singleton has no name, so its field names the user
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheFieldOfTheProjectCreatorSettings()
    {
        _servers.Setup(r => r.GetByName("dl360", It.IsAny<CancellationToken>())).ReturnsAsync(_server);
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings(_server));

        Result result = await Handle("dl360", 3);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Server dl360 Is Used By: ProjectCreatorSettings.ProductionServerName", result.Error.Description);
        VerifyNothingDeleted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _servers.Setup(r => r.GetByName("dl360", It.IsAny<CancellationToken>())).ReturnsAsync(_server);

        Result result = await Handle("dl360", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"Server dl360 Version Conflict: Expected {version}, Actual 3", result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _servers.Setup(r => r.GetByName("dl360", It.IsAny<CancellationToken>())).ReturnsAsync(_server);

        Result result = await Handle("dl360", null);

        Assert.True(result.IsSuccess);
        _servers.Verify(r => r.Delete(_server), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _servers.SetupSequence(r => r.GetByName("dl360", It.IsAny<CancellationToken>())).ReturnsAsync(_server)
            .ReturnsAsync(TestData.NewServer("dl360", version: 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("dl360", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Server dl360 Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _servers.SetupSequence(r => r.GetByName("dl360", It.IsAny<CancellationToken>())).ReturnsAsync(_server)
            .ReturnsAsync((Server?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("dl360", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
