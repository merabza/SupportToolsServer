using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Runtimes.DeleteRuntime;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.Runtimes;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Runtimes;

public sealed class DeleteRuntimeCommandHandlerTests
{
    private readonly Runtime _runtime = TestData.NewRuntime("win-x64", "Windows x64", 3);
    private readonly Mock<IRuntimeRepository> _runtimes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteRuntimeCommandHandler(_runtimes.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteRuntimeCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _runtimes.Verify(r => r.Delete(It.IsAny<Runtime>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _runtimes.Setup(r => r.GetByName("osx-arm64", It.IsAny<CancellationToken>())).ReturnsAsync((Runtime?)null);

        Result result = await Handle("osx-arm64", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Runtime With Name osx-arm64 Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        _runtimes.Setup(r => r.GetByName("WIN-X64", It.IsAny<CancellationToken>())).ReturnsAsync(_runtime);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("WIN-X64", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _runtimes.Verify(r => r.Delete(_runtime), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _runtimes.Setup(r => r.GetByName("win-x64", It.IsAny<CancellationToken>())).ReturnsAsync(_runtime);

        Result result = await Handle("win-x64", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"Runtime win-x64 Version Conflict: Expected {version}, Actual 3", result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _runtimes.Setup(r => r.GetByName("win-x64", It.IsAny<CancellationToken>())).ReturnsAsync(_runtime);

        Result result = await Handle("win-x64", null);

        Assert.True(result.IsSuccess);
        _runtimes.Verify(r => r.Delete(_runtime), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _runtimes.SetupSequence(r => r.GetByName("win-x64", It.IsAny<CancellationToken>())).ReturnsAsync(_runtime)
            .ReturnsAsync(TestData.NewRuntime("win-x64", "Changed", 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("win-x64", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Runtime win-x64 Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _runtimes.SetupSequence(r => r.GetByName("win-x64", It.IsAny<CancellationToken>())).ReturnsAsync(_runtime)
            .ReturnsAsync((Runtime?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("win-x64", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
