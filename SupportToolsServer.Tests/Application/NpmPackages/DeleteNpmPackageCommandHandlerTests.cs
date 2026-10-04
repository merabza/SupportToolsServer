using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.NpmPackages.DeleteNpmPackage;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.NpmPackages;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.NpmPackages;

public sealed class DeleteNpmPackageCommandHandlerTests
{
    private readonly NpmPackage _npmPackage = TestData.NewNpmPackage("react", "UI library", 3);
    private readonly Mock<INpmPackageRepository> _npmPackages = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteNpmPackageCommandHandler(_npmPackages.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteNpmPackageCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _npmPackages.Verify(r => r.Delete(It.IsAny<NpmPackage>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _npmPackages.Setup(r => r.GetByName("left-pad", It.IsAny<CancellationToken>())).ReturnsAsync((NpmPackage?)null);

        Result result = await Handle("left-pad", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("NpmPackage With Name left-pad Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        _npmPackages.Setup(r => r.GetByName("REACT", It.IsAny<CancellationToken>())).ReturnsAsync(_npmPackage);
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("REACT", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _npmPackages.Verify(r => r.Delete(_npmPackage), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _npmPackages.Setup(r => r.GetByName("react", It.IsAny<CancellationToken>())).ReturnsAsync(_npmPackage);

        Result result = await Handle("react", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"NpmPackage react Version Conflict: Expected {version}, Actual 3", result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _npmPackages.Setup(r => r.GetByName("react", It.IsAny<CancellationToken>())).ReturnsAsync(_npmPackage);

        Result result = await Handle("react", null);

        Assert.True(result.IsSuccess);
        _npmPackages.Verify(r => r.Delete(_npmPackage), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _npmPackages.SetupSequence(r => r.GetByName("react", It.IsAny<CancellationToken>())).ReturnsAsync(_npmPackage)
            .ReturnsAsync(TestData.NewNpmPackage("react", "Changed", 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("react", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("NpmPackage react Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _npmPackages.SetupSequence(r => r.GetByName("react", It.IsAny<CancellationToken>())).ReturnsAsync(_npmPackage)
            .ReturnsAsync((NpmPackage?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("react", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
