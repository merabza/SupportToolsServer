using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Settings.UpdateGlobalSettings;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Settings;

//The license key is made up
public sealed class UpdateGlobalSettingsCommandHandlerTests
{
    private readonly Mock<IApiClientRepository> _apiClients = new();
    private readonly FileStorage _backups = TestData.NewFileStorage("Backups");
    private readonly FileStorage _exchange = TestData.NewFileStorage("Exchange");
    private readonly Mock<IFileStorageRepository> _fileStorages = new();
    private readonly Mock<IGlobalSettingsRepository> _globalSettings = new();
    private readonly SmartSchema _keep = TestData.NewSmartSchema("Keep");
    private readonly ApiClient _packages = TestData.NewApiClient("Bagetter");
    private readonly SmartSchema _reduce = TestData.NewSmartSchema("Reduce");
    private readonly Mock<ISmartSchemaRepository> _smartSchemas = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateGlobalSettingsCommandHandlerTests()
    {
        _fileStorages.Setup(r => r.GetByName("Exchange", It.IsAny<CancellationToken>())).ReturnsAsync(_exchange);
        _fileStorages.Setup(r => r.GetByName("Backups", It.IsAny<CancellationToken>())).ReturnsAsync(_backups);
        _fileStorages.Setup(r => r.GetByName("Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileStorage?)null);
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>())).ReturnsAsync(_reduce);
        _smartSchemas.Setup(r => r.GetByName("Keep", It.IsAny<CancellationToken>())).ReturnsAsync(_keep);
        _smartSchemas.Setup(r => r.GetByName("Hourly", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartSchema?)null);
        _smartSchemas.Setup(r => r.GetByName("Weekly", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartSchema?)null);
        _apiClients.Setup(r => r.GetByName("Bagetter", It.IsAny<CancellationToken>())).ReturnsAsync(_packages);
        _apiClients.Setup(r => r.GetByName("Pc9.Packages", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiClient?)null);
    }

    private static StsGlobalSettingsDataModel ModelWithEveryReference(int version = 0)
    {
        return TestData.GlobalSettingsModel("Exchange", "Reduce", "Keep", "Bagetter", "Backups", "Reduce", "Keep",
            version);
    }

    private void GivenStored(GlobalSettings? stored)
    {
        _globalSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsGlobalSettingsDataModel model, CancellationToken cancellationToken = default)
    {
        var handler = new UpdateGlobalSettingsCommandHandler(_globalSettings.Object, _fileStorages.Object,
            _smartSchemas.Object, _apiClients.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateGlobalSettingsCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _globalSettings.Verify(r => r.Add(It.IsAny<GlobalSettings>()), Times.Never);
        _globalSettings.Verify(r => r.Update(It.IsAny<GlobalSettings>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    //The first create: version 0 is expected, the singleton gets its fixed key and the first version
    [Fact]
    public async Task Handle_CreatesTheSingletonWithItsReferencesAndTheFirstVersion()
    {
        GivenStored(null);
        GlobalSettings? added = null;
        _globalSettings.Setup(r => r.Add(It.IsAny<GlobalSettings>())).Callback<GlobalSettings>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle(ModelWithEveryReference(), cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal(GlobalSettingsId.Singleton, added.Id);
        Assert.Equal("ltgmz", added.ServiceDescriptionSignature);
        Assert.Equal(".up!", added.UploadTempExtension);
        Assert.Equal("yyyyMMddHHmmss", added.ProgramArchiveDateMask);
        Assert.Equal(".zip", added.ProgramArchiveExtension);
        Assert.Equal("yyyyMMdd", added.ParametersFileDateMask);
        Assert.Equal(".json", added.ParametersFileExtension);
        Assert.Equal(TestData.MadeUpLicenseKey, added.MediatRLicenseKey);
        Assert.Equal(_exchange.Id, added.FileStorageForExchangeId);
        Assert.Equal(_reduce.Id, added.SmartSchemaForExchangeId);
        Assert.Equal(_keep.Id, added.SmartSchemaForLocalId);
        Assert.Equal(_packages.Id, added.LocalPackageManagerWebApiClientId);
        Assert.Equal(".down!", added.DatabasesBackupFilesExchange.DownloadTempExtension);
        Assert.Equal(".up!", added.DatabasesBackupFilesExchange.UploadTempExtension);
        Assert.Equal(_backups.Id, added.DatabasesBackupFilesExchange.ExchangeFileStorageId);
        Assert.Equal(_reduce.Id, added.DatabasesBackupFilesExchange.ExchangeSmartSchemaId);
        Assert.Equal(_keep.Id, added.DatabasesBackupFilesExchange.LocalSmartSchemaId);
        Assert.Equal(1, added.Version);
        _globalSettings.Verify(r => r.Get(cancellation.Token), Times.Once);
        _fileStorages.Verify(r => r.GetByName("Exchange", cancellation.Token), Times.Once);
        _fileStorages.Verify(r => r.GetByName("Backups", cancellation.Token), Times.Once);
        _smartSchemas.Verify(r => r.GetByName("Reduce", cancellation.Token), Times.Exactly(2));
        _smartSchemas.Verify(r => r.GetByName("Keep", cancellation.Token), Times.Exactly(2));
        _apiClients.Verify(r => r.GetByName("Bagetter", cancellation.Token), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //No name is no reference: the referenced records are not even read
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Handle_StoresNoReferences_WhenTheBodyNamesNone(string? name)
    {
        GivenStored(null);
        GlobalSettings? added = null;
        _globalSettings.Setup(r => r.Add(It.IsAny<GlobalSettings>())).Callback<GlobalSettings>(x => added = x);

        Result<int> result = await Handle(TestData.GlobalSettingsModel(name, name, name, name, name, name, name));

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Null(added.FileStorageForExchangeId);
        Assert.Null(added.SmartSchemaForExchangeId);
        Assert.Null(added.SmartSchemaForLocalId);
        Assert.Null(added.LocalPackageManagerWebApiClientId);
        Assert.Null(added.DatabasesBackupFilesExchange.ExchangeFileStorageId);
        Assert.Null(added.DatabasesBackupFilesExchange.ExchangeSmartSchemaId);
        Assert.Null(added.DatabasesBackupFilesExchange.LocalSmartSchemaId);
        _fileStorages.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _smartSchemas.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _apiClients.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    //Every missing name comes at once, grouped by the type of the referenced record in the order of the contract;
    //a name missing in two fields is named once
    [Fact]
    public async Task Handle_ReturnsReferencedRecordsNotFoundWithEveryMissingName()
    {
        GivenStored(TestData.NewGlobalSettings(version: 2));

        Result<int> result = await Handle(TestData.GlobalSettingsModel("Missing", "Hourly", "Reduce", "Pc9.Packages",
            "Missing", "Hourly", "Weekly", 2));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Referenced FileStorage Records Not Found: Missing; " +
                     "Referenced SmartSchema Records Not Found: Hourly, Weekly; " +
                     "Referenced ApiClient Records Not Found: Pc9.Packages", result.Error.Description);
        VerifyNothingSaved();
    }

    //The references of the exchange parameters are checked like the others
    [Fact]
    public async Task Handle_ReturnsReferencedRecordsNotFound_WhenOnlyAnExchangeReferenceIsMissing()
    {
        GivenStored(null);

        Result<int> result = await Handle(TestData.GlobalSettingsModel(exchangeFileStorageName: "Missing"));

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal("Referenced FileStorage Records Not Found: Missing", result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheSingletonExists()
    {
        GivenStored(TestData.NewGlobalSettings(version: 2));

        Result<int> result = await Handle(TestData.GlobalSettingsModel());

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Settings Global Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
    }

    //The version is checked first: a stale request reads no referenced record, even a missing one
    [Fact]
    public async Task Handle_ChecksTheVersionBeforeTheReferences()
    {
        GivenStored(TestData.NewGlobalSettings(version: 3));

        Result<int> result = await Handle(TestData.GlobalSettingsModel("Missing", version: 2));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        _fileStorages.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingSaved();
    }

    //The update replaces every value, the exchange parameters included, and keeps the fixed key
    [Fact]
    public async Task Handle_UpdatesTheStoredSingletonAndReturnsItsNextVersion()
    {
        GlobalSettings stored = TestData.NewGlobalSettings(_exchange, _reduce, _keep, _packages, _backups, _reduce,
            _keep, 3);
        GivenStored(stored);
        StsGlobalSettingsDataModel model = TestData.GlobalSettingsModel("Backups", version: 3);
        model.MediatRLicenseKey = null;
        model.DatabasesBackupFilesExchange.UploadTempExtension = ".upload";

        Result<int> result = await Handle(model);

        Assert.Equal(4, result.Value);
        _globalSettings.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal(GlobalSettingsId.Singleton, stored.Id);
        Assert.Null(stored.MediatRLicenseKey);
        Assert.Equal(_backups.Id, stored.FileStorageForExchangeId);
        Assert.Null(stored.SmartSchemaForExchangeId);
        Assert.Null(stored.SmartSchemaForLocalId);
        Assert.Null(stored.LocalPackageManagerWebApiClientId);
        Assert.Equal(".upload", stored.DatabasesBackupFilesExchange.UploadTempExtension);
        Assert.Null(stored.DatabasesBackupFilesExchange.ExchangeFileStorageId);
        Assert.Null(stored.DatabasesBackupFilesExchange.ExchangeSmartSchemaId);
        Assert.Null(stored.DatabasesBackupFilesExchange.LocalSmartSchemaId);
        Assert.Equal(4, stored.Version);
        _globalSettings.Verify(r => r.Add(It.IsAny<GlobalSettings>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored(TestData.NewGlobalSettings(version: 3));

        Result<int> result = await Handle(TestData.GlobalSettingsModel(version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"Settings Global Version Conflict: Expected {expectedVersion}, Actual 3",
            result.Error.Description);
        VerifyNothingSaved();
    }

    //A client that saw a version of another database expects a record that is not there
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoSingleton()
    {
        GivenStored(null);

        Result<int> result = await Handle(TestData.GlobalSettingsModel(version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Settings With Name Global Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSingletonChangesBeforeTheSave()
    {
        _globalSettings.SetupSequence(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewGlobalSettings()).ReturnsAsync(TestData.NewGlobalSettings(version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.GlobalSettingsModel(version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Settings Global Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    //Two first creates at once: the fixed key stops the second INSERT, which reads the stored version again
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSingletonIsCreatedBeforeTheSave()
    {
        _globalSettings.SetupSequence(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync((GlobalSettings?)null).ReturnsAsync(TestData.NewGlobalSettings());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("primary key"));

        Result<int> result = await Handle(TestData.GlobalSettingsModel());

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Settings Global Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }

    //A save that fails for another reason (here: a referenced record was deleted after it was read) keeps its
    //exception. The second read is another instance, as from the database: the handler incremented the version of the
    //first one
    [Fact]
    public async Task Handle_ThrowsTheSaveException_WhenTheVersionStillMatches()
    {
        _globalSettings.SetupSequence(r => r.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewGlobalSettings()).ReturnsAsync(TestData.NewGlobalSettings());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("foreign key"));

        await Assert.ThrowsAsync<DbUpdateException>(() => Handle(ModelWithEveryReference(1)));
    }
}
