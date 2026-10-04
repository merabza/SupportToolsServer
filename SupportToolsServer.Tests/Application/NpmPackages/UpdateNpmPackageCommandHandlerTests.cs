using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.NpmPackages.UpdateNpmPackage;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.NpmPackages;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.NpmPackages;

public sealed class UpdateNpmPackageCommandHandlerTests
{
    private readonly Mock<INpmPackageRepository> _npmPackages = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void GivenStored(string name, NpmPackage? stored)
    {
        _npmPackages.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(string name, string? description, int version,
        CancellationToken cancellationToken = default)
    {
        var handler = new UpdateNpmPackageCommandHandler(_npmPackages.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateNpmPackageCommand(TestData.NpmPackageModel(name, description, version)),
            cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _npmPackages.Verify(r => r.Add(It.IsAny<NpmPackage>()), Times.Never);
        _npmPackages.Verify(r => r.Update(It.IsAny<NpmPackage>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithTheFirstVersion_WhenVersionZeroIsExpected()
    {
        GivenStored("react", null);
        NpmPackage? added = null;
        _npmPackages.Setup(r => r.Add(It.IsAny<NpmPackage>())).Callback<NpmPackage>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle("react", "UI library", 0, cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("react", added.Name);
        Assert.Equal("UI library", added.Description);
        Assert.Equal(1, added.Version);
        _npmPackages.Verify(r => r.Update(It.IsAny<NpmPackage>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("react", TestData.NewNpmPackage("REACT", "UI library", 2));

        Result<int> result = await Handle("react", "UI library", 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("NpmPackage react Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The name is matched without case, so the update also takes the spelling of the route key
    [Fact]
    public async Task Handle_UpdatesTheStoredRecordAndReturnsItsNextVersion_WhenTheExpectedVersionIsStored()
    {
        NpmPackage stored = TestData.NewNpmPackage("react", "Old", 3);
        GivenStored("REACT", stored);

        Result<int> result = await Handle("REACT", null, 3);

        Assert.Equal(4, result.Value);
        _npmPackages.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("REACT", stored.Name);
        Assert.Null(stored.Description);
        Assert.Equal(4, stored.Version);
        _npmPackages.Verify(r => r.Add(It.IsAny<NpmPackage>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("react", TestData.NewNpmPackage("react", "UI library", 3));

        Result<int> result = await Handle("react", "New", expectedVersion);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"NpmPackage react Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("react", null);

        Result<int> result = await Handle("react", "UI library", 2);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("NpmPackage With Name react Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save, so the concurrency token stopped the UPDATE
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _npmPackages.SetupSequence(r => r.GetByName("react", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewNpmPackage("react", "UI library"))
            .ReturnsAsync(TestData.NewNpmPackage("react", "Changed", 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle("react", "Mine", 1);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("NpmPackage react Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        _npmPackages.SetupSequence(r => r.GetByName("react", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NpmPackage?)null).ReturnsAsync(TestData.NewNpmPackage("react", "Theirs"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle("react", "Mine", 0);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("NpmPackage react Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }
}
