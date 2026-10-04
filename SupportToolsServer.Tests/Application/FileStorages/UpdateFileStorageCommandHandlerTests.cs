using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.FileStorages.UpdateFileStorage;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.FileStorages;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.FileStorages;

public sealed class UpdateFileStorageCommandHandlerTests
{
    private readonly Mock<IFileStorageRepository> _fileStorages = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void GivenStored(string name, FileStorage? stored)
    {
        _fileStorages.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsFileStorageDataModel model, CancellationToken cancellationToken = default)
    {
        var handler = new UpdateFileStorageCommandHandler(_fileStorages.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateFileStorageCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _fileStorages.Verify(r => r.Add(It.IsAny<FileStorage>()), Times.Never);
        _fileStorages.Verify(r => r.Update(It.IsAny<FileStorage>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithTheFirstVersion_WhenVersionZeroIsExpected()
    {
        GivenStored("Exchange", null);
        FileStorage? added = null;
        _fileStorages.Setup(r => r.Add(It.IsAny<FileStorage>())).Callback<FileStorage>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle(TestData.FileStorageModel("Exchange"), cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("Exchange", added.Name);
        Assert.Equal("ftp://ftp.example.com/x/", added.FileStoragePath);
        Assert.Equal(TestData.MadeUpUser, added.UserName);
        Assert.Equal(TestData.MadeUpPassword, added.Password);
        Assert.Equal(255, added.FileNameMaxLength);
        Assert.Equal(4, added.FileSizeSplitPositionInRow);
        Assert.Equal(1, added.FtpSiteLsFileOffset);
        Assert.Equal(1, added.Version);
        _fileStorages.Verify(r => r.Update(It.IsAny<FileStorage>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("Exchange", TestData.NewFileStorage("EXCHANGE", version: 2));

        Result<int> result = await Handle(TestData.FileStorageModel("Exchange"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("FileStorage Exchange Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The name is matched without case, so the update also takes the spelling of the route key
    [Fact]
    public async Task Handle_UpdatesTheStoredRecordAndReturnsItsNextVersion_WhenTheExpectedVersionIsStored()
    {
        FileStorage stored = TestData.NewFileStorage("Exchange", version: 3);
        GivenStored("EXCHANGE", stored);

        Result<int> result = await Handle(TestData.FileStorageModel("EXCHANGE", @"D:\Bak", null, null, 3));

        Assert.Equal(4, result.Value);
        _fileStorages.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("EXCHANGE", stored.Name);
        Assert.Equal(@"D:\Bak", stored.FileStoragePath);
        Assert.Null(stored.UserName);
        Assert.Null(stored.Password);
        Assert.Equal(4, stored.Version);
        _fileStorages.Verify(r => r.Add(It.IsAny<FileStorage>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("Exchange", TestData.NewFileStorage("Exchange", version: 3));

        Result<int> result = await Handle(TestData.FileStorageModel("Exchange", version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"FileStorage Exchange Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("Exchange", null);

        Result<int> result = await Handle(TestData.FileStorageModel("Exchange", version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("FileStorage With Name Exchange Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save, so the concurrency token stopped the UPDATE
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _fileStorages.SetupSequence(r => r.GetByName("Exchange", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewFileStorage("Exchange"))
            .ReturnsAsync(TestData.NewFileStorage("Exchange", version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.FileStorageModel("Exchange", version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("FileStorage Exchange Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        _fileStorages.SetupSequence(r => r.GetByName("Exchange", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileStorage?)null).ReturnsAsync(TestData.NewFileStorage("Exchange"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle(TestData.FileStorageModel("Exchange"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("FileStorage Exchange Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }
}
