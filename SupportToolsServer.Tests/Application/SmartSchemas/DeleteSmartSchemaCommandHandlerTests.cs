using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.SmartSchemas;

public sealed class DeleteSmartSchemaCommandHandlerTests
{
    private readonly Mock<IGlobalSettingsRepository> _globalSettings = new();
    private readonly Mock<IProjectCreatorSettingsRepository> _projectCreatorSettings = new();
    private readonly SmartSchema _smartSchema = TestData.NewSmartSchema("Reduce", 1, [("Day", 3)], 3);
    private readonly Mock<ISmartSchemaRepository> _smartSchemas = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Task<Result> Handle(string name, int? version, CancellationToken cancellationToken = default)
    {
        var handler = new DeleteSmartSchemaCommandHandler(_smartSchemas.Object, _globalSettings.Object,
            _projectCreatorSettings.Object, _unitOfWork.Object);
        return handler.Handle(new DeleteSmartSchemaCommand(name, version), cancellationToken);
    }

    private void VerifyNothingDeleted()
    {
        _smartSchemas.Verify(r => r.Delete(It.IsAny<SmartSchema>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _smartSchemas.Setup(r => r.GetByName("Hourly", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartSchema?)null);

        Result result = await Handle("Hourly", 1);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("SmartSchema With Name Hourly Not Found", result.Error.Description);
        VerifyNothingDeleted();
    }

    //Settings that name another schema do not use it
    [Fact]
    public async Task Handle_DeletesTheRecord_WhenTheExpectedVersionIsStored()
    {
        SmartSchema other = TestData.NewSmartSchema("Keep");
        _smartSchemas.Setup(r => r.GetByName("REDUCE", It.IsAny<CancellationToken>())).ReturnsAsync(_smartSchema);
        _globalSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync(
            TestData.NewGlobalSettings(smartSchemaForExchange: other, smartSchemaForLocal: other,
                exchangeSmartSchema: other, localSmartSchema: other));
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings(useSmartSchema: other));
        using var cancellation = new CancellationTokenSource();

        Result result = await Handle("REDUCE", 3, cancellation.Token);

        Assert.True(result.IsSuccess);
        _globalSettings.Verify(r => r.Get(cancellation.Token), Times.Once);
        _projectCreatorSettings.Verify(r => r.Get(cancellation.Token), Times.Once);
        _smartSchemas.Verify(r => r.Delete(_smartSchema), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //The singletons have no name, so their fields name the users, in the order of the contracts
    [Fact]
    public async Task Handle_ReturnsRecordIsInUseWithTheFieldsOfTheSettingsThatUseIt()
    {
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>())).ReturnsAsync(_smartSchema);
        _globalSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync(
            TestData.NewGlobalSettings(smartSchemaForExchange: _smartSchema, smartSchemaForLocal: _smartSchema,
                exchangeSmartSchema: _smartSchema, localSmartSchema: _smartSchema));
        _projectCreatorSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectCreatorSettings(useSmartSchema: _smartSchema));

        Result result = await Handle("Reduce", 3);

        Assert.Equal("RecordIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(
            "SmartSchema Reduce Is Used By: GlobalSettings.SmartSchemaNameForExchange, " +
            "GlobalSettings.SmartSchemaNameForLocal, " +
            "GlobalSettings.DatabasesBackupFilesExchange.ExchangeSmartSchemaName, " +
            "GlobalSettings.DatabasesBackupFilesExchange.LocalSmartSchemaName, ProjectCreatorSettings.UseSmartSchema",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    [Fact]
    public async Task Handle_ReturnsRecordIsInUse_WhenOnlyTheLocalSchemaOfTheExchangeParametersUsesIt()
    {
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>())).ReturnsAsync(_smartSchema);
        _globalSettings.Setup(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewGlobalSettings(localSmartSchema: _smartSchema));

        Result result = await Handle("Reduce", 3);

        Assert.Equal("SmartSchema Reduce Is Used By: GlobalSettings.DatabasesBackupFilesExchange.LocalSmartSchemaName",
            result.Error.Description);
        VerifyNothingDeleted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflict_WhenAnotherVersionIsStored(int version)
    {
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>())).ReturnsAsync(_smartSchema);

        Result result = await Handle("Reduce", version);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal($"SmartSchema Reduce Version Conflict: Expected {version}, Actual 3", result.Error.Description);
        VerifyNothingDeleted();
    }

    //The manual editors delete without a version
    [Fact]
    public async Task Handle_DeletesTheRecordOfAnyVersion_WithoutAnExpectedVersion()
    {
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>())).ReturnsAsync(_smartSchema);

        Result result = await Handle("Reduce", null);

        Assert.True(result.IsSuccess);
        _smartSchemas.Verify(r => r.Delete(_smartSchema), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    //The DELETE is based on the version that was read, also without an expected version
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheRecordChangesBeforeTheSave()
    {
        _smartSchemas.SetupSequence(r => r.GetByName("Reduce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_smartSchema).ReturnsAsync(TestData.NewSmartSchema("Reduce", version: 4));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Reduce", null);

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("SmartSchema Reduce Version Conflict: Expected 3, Actual 4", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenTheRecordIsDeletedBeforeTheSave()
    {
        _smartSchemas.SetupSequence(r => r.GetByName("Reduce", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_smartSchema).ReturnsAsync((SmartSchema?)null);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result result = await Handle("Reduce", 3);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }
}
