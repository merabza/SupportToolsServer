using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Environments.DeleteEnvironment;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Environments;

public sealed class DeleteEnvironmentCommandHandlerTests
{
    private readonly Mock<IDeploymentEnvironmentRepository> _environments = new();
    private readonly DeploymentEnvironment _prod = TestData.NewEnvironment("Prod", "Production", 3);
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteEnvironmentCommandHandler(_environments.Object, _unitOfWork.Object);
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

    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        _environments.Setup(r => r.GetByName("prod", It.IsAny<CancellationToken>())).ReturnsAsync(_prod);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("prod", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _environments.Verify(r => r.Delete(_prod), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
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
