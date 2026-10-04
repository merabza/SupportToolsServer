using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.SmartSchemas.UpdateSmartSchema;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.SmartSchemas;

public sealed class UpdateSmartSchemaCommandHandlerTests
{
    private readonly Mock<ISmartSchemaRepository> _smartSchemas = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    //The handler reads the schema with tracking, so that the save deletes the replaced details
    private void GivenStored(string name, SmartSchema? stored)
    {
        _smartSchemas.Setup(r => r.GetByNameForUpdate(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsSmartSchemaDataModel model, CancellationToken cancellationToken = default)
    {
        var handler = new UpdateSmartSchemaCommandHandler(_smartSchemas.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateSmartSchemaCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _smartSchemas.Verify(r => r.Add(It.IsAny<SmartSchema>()), Times.Never);
        _smartSchemas.Verify(r => r.Update(It.IsAny<SmartSchema>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreatesTheRecordWithItsDetailsAndTheFirstVersion_WhenVersionZeroIsExpected()
    {
        GivenStored("Reduce", null);
        SmartSchema? added = null;
        _smartSchemas.Setup(r => r.Add(It.IsAny<SmartSchema>())).Callback<SmartSchema>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle(TestData.SmartSchemaModel("Reduce", 2, [("Day", 3), ("Month", 1)]),
            cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("Reduce", added.Name);
        Assert.Equal(2, added.LastPreserveCount);
        Assert.Equal(["Day", "Month"], added.Details.Select(x => x.PeriodType));
        Assert.Equal([3, 1], added.Details.Select(x => x.PreserveCount));
        Assert.Equal(1, added.Version);
        _smartSchemas.Verify(r => r.Update(It.IsAny<SmartSchema>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("Reduce", TestData.NewSmartSchema("REDUCE", version: 2));

        Result<int> result = await Handle(TestData.SmartSchemaModel("Reduce"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("SmartSchema Reduce Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The update replaces the whole aggregate: the stored details are gone, the details of the body are the only ones
    [Fact]
    public async Task Handle_ReplacesTheStoredRecordWithItsDetailsAndReturnsItsNextVersion()
    {
        SmartSchema stored = TestData.NewSmartSchema("Reduce", 1, [("Day", 3), ("Week", 2)], 3);
        GivenStored("REDUCE", stored);

        Result<int> result = await Handle(TestData.SmartSchemaModel("REDUCE", 4, [("Day", 7), ("Hour", 48)], 3));

        Assert.Equal(4, result.Value);
        _smartSchemas.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("REDUCE", stored.Name);
        Assert.Equal(4, stored.LastPreserveCount);
        Assert.Equal(["Day", "Hour"], stored.Details.Select(x => x.PeriodType));
        Assert.Equal([7, 48], stored.Details.Select(x => x.PreserveCount));
        Assert.Equal(4, stored.Version);
        _smartSchemas.Verify(r => r.Add(It.IsAny<SmartSchema>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RemovesEveryDetail_WhenTheBodyHasNone()
    {
        SmartSchema stored = TestData.NewSmartSchema("Reduce", 1, [("Day", 3)], 3);
        GivenStored("Reduce", stored);

        Result<int> result = await Handle(TestData.SmartSchemaModel("Reduce", 1, [], 3));

        Assert.Equal(4, result.Value);
        Assert.Empty(stored.Details);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("Reduce", TestData.NewSmartSchema("Reduce", version: 3));

        Result<int> result = await Handle(TestData.SmartSchemaModel("Reduce", version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"SmartSchema Reduce Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //The record was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoRecord()
    {
        GivenStored("Reduce", null);

        Result<int> result = await Handle(TestData.SmartSchemaModel("Reduce", version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("SmartSchema With Name Reduce Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save. The version is read again without tracking, so it
    //is the stored one and not the version of the schema this request changed
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        GivenStored("Reduce", TestData.NewSmartSchema("Reduce"));
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewSmartSchema("Reduce", 5, version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.SmartSchemaModel("Reduce", version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("SmartSchema Reduce Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        GivenStored("Reduce", null);
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewSmartSchema("Reduce"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle(TestData.SmartSchemaModel("Reduce"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("SmartSchema Reduce Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        GivenStored("Reduce", TestData.NewSmartSchema("Reduce"));
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartSchema?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.SmartSchemaModel("Reduce", version: 1));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
