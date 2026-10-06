using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.StoredFiles.DeleteStoredFile;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.StoredFiles;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.StoredFiles;

//No other aggregate references a stored file, so there is no usage check
public sealed class DeleteStoredFileCommandHandlerTests
{
    private const string FilePath = @"D:\1WorkSecurity\AppA\appsettings.json";

    private readonly StoredFile _stored = TestData.NewStoredFile(FilePath, version: 3);
    private readonly Mock<IStoredFileRepository> _storedFiles = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string path, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteStoredFileCommandHandler(_storedFiles.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteStoredFileCommand(path, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _storedFiles.Verify(r => r.Delete(It.IsAny<StoredFile>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchPath()
    {
        _storedFiles.Setup(r => r.GetByPath(@"D:\x.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null);

        Result result = await Handle(@"D:\x.json", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(@"StoredFile With Name D:\x.json Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    [Fact]
    public async Task Handle_DeletesTheFile_WhenTheExpectedVersionIsStored()
    {
        _storedFiles.Setup(r => r.GetByPath(FilePath.ToUpperInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_stored);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle(FilePath.ToUpperInvariant(), 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _storedFiles.Verify(r => r.GetByPath(FilePath.ToUpperInvariant(), cancellation.Token), Times.Once);
        _storedFiles.Verify(r => r.Delete(_stored), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _storedFiles.Setup(r => r.GetByPath(FilePath, It.IsAny<CancellationToken>())).ReturnsAsync(_stored);

        Result result = await Handle(FilePath, version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected {version}, Actual 3",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheFileOfAnyVersion_WithoutAnExpectedVersion()
    {
        _storedFiles.Setup(r => r.GetByPath(FilePath, It.IsAny<CancellationToken>())).ReturnsAsync(_stored);

        Result result = await Handle(FilePath, null);

        Assert.True(result.IsSuccess);
        _storedFiles.Verify(r => r.Delete(_stored), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheFileChangesBeforeTheSave()
    {
        _storedFiles.SetupSequence(r => r.GetByPath(FilePath, It.IsAny<CancellationToken>())).ReturnsAsync(_stored)
            .ReturnsAsync(TestData.NewStoredFile(FilePath, "changed", 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle(FilePath, null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheFileIsDeletedBeforeTheSave()
    {
        _storedFiles.SetupSequence(r => r.GetByPath(FilePath, It.IsAny<CancellationToken>())).ReturnsAsync(_stored)
            .ReturnsAsync((StoredFile?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle(FilePath, 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
