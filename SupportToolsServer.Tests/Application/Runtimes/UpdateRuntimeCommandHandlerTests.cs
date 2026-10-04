using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Runtimes.UpdateRuntime;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.Runtimes;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Runtimes;

public sealed class UpdateRuntimeCommandHandlerTests
{
    private readonly Mock<IRuntimeRepository> _runtimes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void GivenStored(string name, Runtime? stored)
    {
        _runtimes.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(string name, string? description, int version,
        CancellationToken cancellationToken = default)
    {
        var handler = new UpdateRuntimeCommandHandler(_runtimes.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateRuntimeCommand(TestData.RuntimeModel(name, description, version)),
            cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _runtimes.Verify(r => r.Add(It.IsAny<Runtime>()), Times.Never);
        _runtimes.Verify(r => r.Update(It.IsAny<Runtime>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithTheFirstVersion_WhenVersionZeroIsExpected()
    {
        GivenStored("win-x64", null);
        Runtime? added = null;
        _runtimes.Setup(r => r.Add(It.IsAny<Runtime>())).Callback<Runtime>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle("win-x64", "Windows x64", 0, cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("win-x64", added.Name);
        Assert.Equal("Windows x64", added.Description);
        Assert.Equal(1, added.Version);
        _runtimes.Verify(r => r.Update(It.IsAny<Runtime>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("win-x64", TestData.NewRuntime("WIN-X64", "Windows x64", 2));

        Result<int> result = await Handle("win-x64", "Windows x64", 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Runtime win-x64 Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The name is matched without case, so the update also takes the spelling of the route key
    [Fact]
    public async Task Handle_UpdatesTheStoredRecordAndReturnsItsNextVersion_WhenTheExpectedVersionIsStored()
    {
        Runtime stored = TestData.NewRuntime("win-x64", "Old", 3);
        GivenStored("WIN-X64", stored);

        Result<int> result = await Handle("WIN-X64", null, 3);

        Assert.Equal(4, result.Value);
        _runtimes.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("WIN-X64", stored.Name);
        Assert.Null(stored.Description);
        Assert.Equal(4, stored.Version);
        _runtimes.Verify(r => r.Add(It.IsAny<Runtime>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("win-x64", TestData.NewRuntime("win-x64", "Windows x64", 3));

        Result<int> result = await Handle("win-x64", "New", expectedVersion);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"Runtime win-x64 Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("win-x64", null);

        Result<int> result = await Handle("win-x64", "Windows x64", 2);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Runtime With Name win-x64 Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save, so the concurrency token stopped the UPDATE
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _runtimes.SetupSequence(r => r.GetByName("win-x64", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewRuntime("win-x64", "Windows x64"))
            .ReturnsAsync(TestData.NewRuntime("win-x64", "Changed", 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle("win-x64", "Mine", 1);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Runtime win-x64 Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        _runtimes.SetupSequence(r => r.GetByName("win-x64", It.IsAny<CancellationToken>())).ReturnsAsync((Runtime?)null)
            .ReturnsAsync(TestData.NewRuntime("win-x64", "Theirs"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle("win-x64", "Mine", 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Runtime win-x64 Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }
}
