using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.FileStorages.DeleteFileStorage;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.FileStorages;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.FileStorages;

public sealed class DeleteFileStorageCommandHandlerTests
{
    private readonly FileStorage _fileStorage = TestData.NewFileStorage("Exchange", version: 3);
    private readonly Mock<IFileStorageRepository> _fileStorages = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteFileStorageCommandHandler(_fileStorages.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteFileStorageCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _fileStorages.Verify(r => r.Delete(It.IsAny<FileStorage>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _fileStorages.Setup(r => r.GetByName("LocalBak", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileStorage?)null);

        Result result = await Handle("LocalBak", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("FileStorage With Name LocalBak Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        _fileStorages.Setup(r => r.GetByName("EXCHANGE", It.IsAny<CancellationToken>())).ReturnsAsync(_fileStorage);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("EXCHANGE", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _fileStorages.Verify(r => r.Delete(_fileStorage), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _fileStorages.Setup(r => r.GetByName("Exchange", It.IsAny<CancellationToken>())).ReturnsAsync(_fileStorage);

        Result result = await Handle("Exchange", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"FileStorage Exchange Version Conflict: Expected {version}, Actual 3", result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _fileStorages.Setup(r => r.GetByName("Exchange", It.IsAny<CancellationToken>())).ReturnsAsync(_fileStorage);

        Result result = await Handle("Exchange", null);

        Assert.True(result.IsSuccess);
        _fileStorages.Verify(r => r.Delete(_fileStorage), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _fileStorages.SetupSequence(r => r.GetByName("Exchange", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fileStorage).ReturnsAsync(TestData.NewFileStorage("Exchange", version: 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Exchange", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("FileStorage Exchange Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _fileStorages.SetupSequence(r => r.GetByName("Exchange", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fileStorage).ReturnsAsync((FileStorage?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Exchange", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
