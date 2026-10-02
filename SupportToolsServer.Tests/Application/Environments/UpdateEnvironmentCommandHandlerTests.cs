using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Environments.UpdateEnvironment;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Environments;

public sealed class UpdateEnvironmentCommandHandlerTests
{
    private readonly Mock<IDeploymentEnvironmentRepository> _environments = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void GivenStored(string name, DeploymentEnvironment? stored)
    {
        _environments.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(string name, string? description, int version,
        CancellationToken cancellationToken = default)
    {
        var handler = new UpdateEnvironmentCommandHandler(_environments.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateEnvironmentCommand(TestData.EnvironmentModel(name, description, version)),
            cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _environments.Verify(r => r.Add(It.IsAny<DeploymentEnvironment>()), Times.Never);
        _environments.Verify(r => r.Update(It.IsAny<DeploymentEnvironment>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithTheFirstVersion_WhenVersionZeroIsExpected()
    {
        GivenStored("Prod", null);
        DeploymentEnvironment? added = null;
        _environments.Setup(r => r.Add(It.IsAny<DeploymentEnvironment>()))
            .Callback<DeploymentEnvironment>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle("Prod", "Production", 0, cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("Prod", added.Name);
        Assert.Equal("Production", added.Description);
        Assert.Equal(1, added.Version);
        _environments.Verify(r => r.Update(It.IsAny<DeploymentEnvironment>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("Prod", TestData.NewEnvironment("prod", "Production", 2));

        Result<int> result = await Handle("Prod", "Production", 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Environment Prod Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The name is matched without case, so the update also takes the spelling of the route key
    [Fact]
    public async Task Handle_UpdatesTheStoredRecordAndReturnsItsNextVersion_WhenTheExpectedVersionIsStored()
    {
        DeploymentEnvironment stored = TestData.NewEnvironment("prod", "Old", 3);
        GivenStored("PROD", stored);

        Result<int> result = await Handle("PROD", null, 3);

        Assert.Equal(4, result.Value);
        _environments.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("PROD", stored.Name);
        Assert.Null(stored.Description);
        Assert.Equal(4, stored.Version);
        _environments.Verify(r => r.Add(It.IsAny<DeploymentEnvironment>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("Prod", TestData.NewEnvironment("Prod", "Production", 3));

        Result<int> result = await Handle("Prod", "New", expectedVersion);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"Environment Prod Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("Prod", null);

        Result<int> result = await Handle("Prod", "Production", 2);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Environment With Name Prod Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save, so the concurrency token stopped the UPDATE
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _environments.SetupSequence(r => r.GetByName("Prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewEnvironment("Prod", "Production"))
            .ReturnsAsync(TestData.NewEnvironment("Prod", "Changed", 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle("Prod", "Mine", 1);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Environment Prod Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        _environments.SetupSequence(r => r.GetByName("Prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeploymentEnvironment?)null).ReturnsAsync(TestData.NewEnvironment("Prod", "Theirs"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle("Prod", "Mine", 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Environment Prod Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }
}
