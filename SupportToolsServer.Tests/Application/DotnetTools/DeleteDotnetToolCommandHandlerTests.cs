using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.DotnetTools.DeleteDotnetTool;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.DotnetTools;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.DotnetTools;

public sealed class DeleteDotnetToolCommandHandlerTests
{
    private readonly DotnetTool _dotnetTool =
        TestData.NewDotnetTool("DotnetEf", "dotnet-ef", null, "Entity Framework", 3);

    private readonly Mock<IDotnetToolRepository> _dotnetTools = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteDotnetToolCommandHandler(_dotnetTools.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteDotnetToolCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _dotnetTools.Verify(r => r.Delete(It.IsAny<DotnetTool>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _dotnetTools.Setup(r => r.GetByName("ReportGenerator", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DotnetTool?)null);

        Result result = await Handle("ReportGenerator", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("DotnetTool With Name ReportGenerator Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        _dotnetTools.Setup(r => r.GetByName("DOTNETEF", It.IsAny<CancellationToken>())).ReturnsAsync(_dotnetTool);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("DOTNETEF", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _dotnetTools.Verify(r => r.Delete(_dotnetTool), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _dotnetTools.Setup(r => r.GetByName("DotnetEf", It.IsAny<CancellationToken>())).ReturnsAsync(_dotnetTool);

        Result result = await Handle("DotnetEf", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"DotnetTool DotnetEf Version Conflict: Expected {version}, Actual 3", result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _dotnetTools.Setup(r => r.GetByName("DotnetEf", It.IsAny<CancellationToken>())).ReturnsAsync(_dotnetTool);

        Result result = await Handle("DotnetEf", null);

        Assert.True(result.IsSuccess);
        _dotnetTools.Verify(r => r.Delete(_dotnetTool), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _dotnetTools.SetupSequence(r => r.GetByName("DotnetEf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dotnetTool).ReturnsAsync(TestData.NewDotnetTool("DotnetEf", version: 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("DotnetEf", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DotnetTool DotnetEf Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _dotnetTools.SetupSequence(r => r.GetByName("DotnetEf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_dotnetTool).ReturnsAsync((DotnetTool?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("DotnetEf", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
