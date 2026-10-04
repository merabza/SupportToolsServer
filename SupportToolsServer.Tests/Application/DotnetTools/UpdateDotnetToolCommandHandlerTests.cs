using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.DotnetTools.UpdateDotnetTool;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DotnetTools;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.DotnetTools;

public sealed class UpdateDotnetToolCommandHandlerTests
{
    private readonly Mock<IDotnetToolRepository> _dotnetTools = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private void GivenStored(string name, DotnetTool? stored)
    {
        _dotnetTools.Setup(r => r.GetByName(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsDotnetToolDataModel model, CancellationToken cancellationToken = default)
    {
        var handler = new UpdateDotnetToolCommandHandler(_dotnetTools.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateDotnetToolCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _dotnetTools.Verify(r => r.Add(It.IsAny<DotnetTool>()), Times.Never);
        _dotnetTools.Verify(r => r.Update(It.IsAny<DotnetTool>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithTheFirstVersion_WhenVersionZeroIsExpected()
    {
        GivenStored("DotnetEf", null);
        DotnetTool? added = null;
        _dotnetTools.Setup(r => r.Add(It.IsAny<DotnetTool>())).Callback<DotnetTool>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle(
            TestData.DotnetToolModel("DotnetEf", "dotnet-ef", "9.0.8", "Entity Framework"), cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("DotnetEf", added.Name);
        Assert.Equal("dotnet-ef", added.PackageId);
        Assert.Equal("9.0.8", added.MaxVersion);
        Assert.Equal("Entity Framework", added.Description);
        Assert.Equal(1, added.Version);
        _dotnetTools.Verify(r => r.Update(It.IsAny<DotnetTool>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("DotnetEf", TestData.NewDotnetTool("DOTNETEF", version: 2));

        Result<int> result = await Handle(TestData.DotnetToolModel("DotnetEf"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("DotnetTool DotnetEf Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The name is matched without case, so the update also takes the spelling of the route key
    [Fact]
    public async Task Handle_UpdatesTheStoredRecordAndReturnsItsNextVersion_WhenTheExpectedVersionIsStored()
    {
        DotnetTool stored = TestData.NewDotnetTool("DotnetEf", "dotnet-ef", "9.0.8", "Old", 3);
        GivenStored("DOTNETEF", stored);

        Result<int> result = await Handle(TestData.DotnetToolModel("DOTNETEF", "Dotnet-Ef", null, null, 3));

        Assert.Equal(4, result.Value);
        _dotnetTools.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("DOTNETEF", stored.Name);
        Assert.Equal("Dotnet-Ef", stored.PackageId);
        Assert.Null(stored.MaxVersion);
        Assert.Null(stored.Description);
        Assert.Equal(4, stored.Version);
        _dotnetTools.Verify(r => r.Add(It.IsAny<DotnetTool>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("DotnetEf", TestData.NewDotnetTool("DotnetEf", version: 3));

        Result<int> result = await Handle(TestData.DotnetToolModel("DotnetEf", version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"DotnetTool DotnetEf Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("DotnetEf", null);

        Result<int> result = await Handle(TestData.DotnetToolModel("DotnetEf", version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("DotnetTool With Name DotnetEf Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save, so the concurrency token stopped the UPDATE
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _dotnetTools.SetupSequence(r => r.GetByName("DotnetEf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewDotnetTool("DotnetEf"))
            .ReturnsAsync(TestData.NewDotnetTool("DotnetEf", description: "Changed", version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.DotnetToolModel("DotnetEf", description: "Mine", version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DotnetTool DotnetEf Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        _dotnetTools.SetupSequence(r => r.GetByName("DotnetEf", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DotnetTool?)null).ReturnsAsync(TestData.NewDotnetTool("DotnetEf"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle(TestData.DotnetToolModel("DotnetEf", description: "Mine"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("DotnetTool DotnetEf Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }
}
