using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Registry;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Registry;

public sealed class RecordVersionsTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public void Check_AcceptsZero_WhenTheRecordDoesNotExist()
    {
        Assert.True(RecordVersions.Check("Environment", "Prod", 0, null).IsSuccess);
    }

    [Fact]
    public void Check_ReturnsRecordWithNameNotFound_WhenTheRecordDoesNotExistButAVersionIsExpected()
    {
        Result result = RecordVersions.Check("Environment", "Prod", 2, null);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Environment With Name Prod Not Found", result.Error.Description);
    }

    [Fact]
    public void Check_AcceptsTheStoredVersion()
    {
        Assert.True(RecordVersions.Check("Environment", "Prod", 3, 3).IsSuccess);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(2, 3)]
    [InlineData(4, 3)]
    public void Check_ReturnsConcurrencyConflict_WhenTheStoredVersionIsAnother(int expectedVersion, int storedVersion)
    {
        Result result = RecordVersions.Check("Environment", "Prod", expectedVersion, storedVersion);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"Environment Prod Version Conflict: Expected {expectedVersion}, Actual {storedVersion}",
            result.Error.Description);
    }

    [Fact]
    public async Task SaveChanges_SavesWithoutReadingTheVersionAgain()
    {
        using var cancellation = new CancellationTokenSource();
        bool read = false;

        Result result = await RecordVersions.SaveChanges(_unitOfWork.Object, "Environment", "Prod", 1, _ =>
        {
            read = true;
            return Task.FromResult<int?>(1);
        }, cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.False(read);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //Another request changed the record after it was read: the concurrency token stopped the UPDATE
    [Fact]
    public async Task SaveChanges_ReturnsConcurrencyConflictWithTheActualVersion_WhenTheRecordChangedMeanwhile()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        using var cancellation = new CancellationTokenSource();
        CancellationToken readToken = CancellationToken.None;

        Result result = await RecordVersions.SaveChanges(_unitOfWork.Object, "Environment", "Prod", 2, token =>
        {
            readToken = token;
            return Task.FromResult<int?>(3);
        }, cancellation.Token);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Environment Prod Version Conflict: Expected 2, Actual 3", result.Error.Description);
        Assert.Equal(cancellation.Token, readToken);
    }

    [Fact]
    public async Task SaveChanges_ReturnsRecordWithNameNotFound_WhenTheRecordWasDeletedMeanwhile()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await RecordVersions.SaveChanges(_unitOfWork.Object, "Environment", "Prod", 2,
            _ => Task.FromResult<int?>(null), CancellationToken.None);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }

    //Two requests created the same name: the unique index stopped the second INSERT
    [Fact]
    public async Task SaveChanges_ReturnsConcurrencyConflict_WhenTheSameNameWasCreatedMeanwhile()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result result = await RecordVersions.SaveChanges(_unitOfWork.Object, "Environment", "Prod", 0,
            _ => Task.FromResult<int?>(1), CancellationToken.None);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Environment Prod Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }

    //The version still matches, so the failure has another reason and must not be hidden
    [Fact]
    public async Task SaveChanges_Rethrows_WhenTheVersionStillMatches()
    {
        var failure = new DbUpdateException("foreign key");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        DbUpdateException thrown = await Assert.ThrowsAsync<DbUpdateException>(() =>
            RecordVersions.SaveChanges(_unitOfWork.Object, "Environment", "Prod", 0, _ => Task.FromResult<int?>(null),
                CancellationToken.None));

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task SaveChanges_DoesNotCatchOtherExceptions()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("other"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RecordVersions.SaveChanges(_unitOfWork.Object, "Environment", "Prod", 1, _ => Task.FromResult<int?>(2),
                CancellationToken.None));
    }
}
