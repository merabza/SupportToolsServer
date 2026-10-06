using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.StoredFiles.UpdateStoredFile;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.StoredFiles;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.StoredFiles;

public sealed class UpdateStoredFileCommandHandlerTests
{
    private const string FilePath = @"D:\1WorkSecurity\AppA\appsettings.json";
    private const string LowerCaseFilePath = @"d:\1worksecurity\appa\appsettings.json";

    //SHA-256 of the UTF-8 bytes of "abc"
    private const string AbcSha256 = "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";

    private static readonly DateTimeOffset Now = new(2026, 10, 7, 9, 30, 15, TimeSpan.Zero);

    private readonly Mock<IStoredFileRepository> _storedFiles = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateStoredFileCommandHandlerTests()
    {
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(Now);
    }

    private void GivenStored(string path, StoredFile? stored)
    {
        _storedFiles.Setup(r => r.GetByPath(path, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(string path, string content, int version,
        CancellationToken cancellationToken = default)
    {
        var handler =
            new UpdateStoredFileCommandHandler(_storedFiles.Object, _timeProvider.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateStoredFileCommand(TestData.StoredFileModel(path, content, version)),
            cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _storedFiles.Verify(r => r.Add(It.IsAny<StoredFile>()), Times.Never);
        _storedFiles.Verify(r => r.Update(It.IsAny<StoredFile>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheFileWithItsHashLengthTimeAndTheFirstVersion_WhenVersionZeroIsExpected()
    {
        GivenStored(FilePath, null);
        StoredFile? added = null;
        _storedFiles.Setup(r => r.Add(It.IsAny<StoredFile>())).Callback<StoredFile>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle(FilePath, "abc", 0, cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal(FilePath, added.Path);
        Assert.Equal("abc", added.Content);
        Assert.Equal(AbcSha256, added.Sha256);
        Assert.Equal(3, added.Length);
        Assert.Equal(Now.UtcDateTime, added.UpdatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, added.UpdatedAtUtc.Kind);
        Assert.Equal(1, added.Version);
        _storedFiles.Verify(r => r.Update(It.IsAny<StoredFile>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButThePathExists()
    {
        GivenStored(FilePath, TestData.NewStoredFile(LowerCaseFilePath, version: 2));

        Result<int> result = await Handle(FilePath, TestData.MadeUpFileContent, 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The path is matched without case, so the update also takes the spelling of the request
    [Fact]
    public async Task Handle_UpdatesTheStoredFileWithItsNewHashLengthAndTime_WhenTheExpectedVersionIsStored()
    {
        StoredFile stored = TestData.NewStoredFile(LowerCaseFilePath, version: 3);
        GivenStored(FilePath, stored);

        Result<int> result = await Handle(FilePath, "abc", 3);

        Assert.Equal(4, result.Value);
        _storedFiles.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal(FilePath, stored.Path);
        Assert.Equal("abc", stored.Content);
        Assert.Equal(AbcSha256, stored.Sha256);
        Assert.Equal(3, stored.Length);
        Assert.Equal(Now.UtcDateTime, stored.UpdatedAtUtc);
        Assert.Equal(4, stored.Version);
        _storedFiles.Verify(r => r.Add(It.IsAny<StoredFile>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored(FilePath, TestData.NewStoredFile(FilePath, version: 3));

        Result<int> result = await Handle(FilePath, "abc", expectedVersion);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The file was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoFile()
    {
        GivenStored(FilePath, null);

        Result<int> result = await Handle(FilePath, "abc", 2);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal($"StoredFile With Name {FilePath} Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save, so the concurrency token stopped the UPDATE
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheFileChangesBeforeTheSave()
    {
        _storedFiles.SetupSequence(r => r.GetByPath(FilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewStoredFile(FilePath)).ReturnsAsync(TestData.NewStoredFile(FilePath, "x", 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(FilePath, "abc", 1);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSamePathIsCreatedBeforeTheSave()
    {
        _storedFiles.SetupSequence(r => r.GetByPath(FilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredFile?)null).ReturnsAsync(TestData.NewStoredFile(FilePath));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle(FilePath, "abc", 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }
}
