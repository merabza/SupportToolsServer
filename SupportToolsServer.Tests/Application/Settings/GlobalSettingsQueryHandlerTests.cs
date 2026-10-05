using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.Settings;
using SupportToolsServer.Application.Settings.GetGlobalSettings;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Settings;

public sealed class GlobalSettingsQueryHandlerTests
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

    public GlobalSettingsQueryHandlerTests()
    {
        _fileStorages.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_backups, TestData.NewFileStorage("Other"), _exchange]);
        _smartSchemas.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_reduce, _keep]);
        _apiClients.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewApiClient("Pc1.WebAgent"), _packages]);
    }

    private Task<Result<StsGlobalSettingsDataModel>> Handle(CancellationToken cancellationToken = default)
    {
        return new GetGlobalSettingsQueryHandler(_globalSettings.Object, _fileStorages.Object, _smartSchemas.Object,
            _apiClients.Object).Handle(new GetGlobalSettingsQuery(), cancellationToken);
    }

    //Before the first create the client reads an empty contract with version 0 instead of an error, and the
    //referenced records are not read
    [Fact]
    public async Task Handle_ReturnsAnEmptyContractWithVersionZero_BeforeTheSingletonIsCreated()
    {
        _globalSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync((GlobalSettings?)null);

        Result<StsGlobalSettingsDataModel> result = await Handle();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Version);
        Assert.Null(result.Value.ServiceDescriptionSignature);
        Assert.Null(result.Value.MediatRLicenseKey);
        Assert.Null(result.Value.FileStorageNameForExchange);
        Assert.NotNull(result.Value.DatabasesBackupFilesExchange);
        Assert.Null(result.Value.DatabasesBackupFilesExchange.ExchangeFileStorageName);
        _fileStorages.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _smartSchemas.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _apiClients.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
    }

    //The references are named, not given by their ids
    [Fact]
    public async Task Handle_ReturnsTheSingletonWithTheNamesOfItsReferencesAndItsVersion()
    {
        _globalSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync(
            TestData.NewGlobalSettings(_exchange, _reduce, _keep, _packages, _backups, _keep, _reduce, 5));
        using var cancellation = new CancellationTokenSource();

        Result<StsGlobalSettingsDataModel> result = await Handle(cancellation.Token);

        StsGlobalSettingsDataModel model = result.Value;
        Assert.Equal("Exchange", model.FileStorageNameForExchange);
        Assert.Equal("Reduce", model.SmartSchemaNameForExchange);
        Assert.Equal("Keep", model.SmartSchemaNameForLocal);
        Assert.Equal("Bagetter", model.LocalPackageManagerWebApiClientName);
        Assert.Equal("Backups", model.DatabasesBackupFilesExchange.ExchangeFileStorageName);
        Assert.Equal("Keep", model.DatabasesBackupFilesExchange.ExchangeSmartSchemaName);
        Assert.Equal("Reduce", model.DatabasesBackupFilesExchange.LocalSmartSchemaName);
        Assert.Equal(5, model.Version);
        _globalSettings.Verify(r => r.Get(cancellation.Token), Times.Once);
        _fileStorages.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _smartSchemas.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _apiClients.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsNoNames_WhenTheSingletonHasNoReferences()
    {
        _globalSettings.Setup(r => r.Get(It.IsAny<CancellationToken>())).ReturnsAsync(TestData.NewGlobalSettings());

        Result<StsGlobalSettingsDataModel> result = await Handle();

        Assert.Null(result.Value.FileStorageNameForExchange);
        Assert.Null(result.Value.SmartSchemaNameForExchange);
        Assert.Null(result.Value.SmartSchemaNameForLocal);
        Assert.Null(result.Value.LocalPackageManagerWebApiClientName);
        Assert.Null(result.Value.DatabasesBackupFilesExchange.ExchangeFileStorageName);
        Assert.Null(result.Value.DatabasesBackupFilesExchange.ExchangeSmartSchemaName);
        Assert.Null(result.Value.DatabasesBackupFilesExchange.LocalSmartSchemaName);
        Assert.Equal(1, result.Value.Version);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        GlobalSettings globalSettings =
            TestData.NewGlobalSettings(_exchange, _reduce, _keep, _packages, _backups, _reduce, _keep, 7);

        StsGlobalSettingsDataModel model = globalSettings.ToContractModel(
            new Dictionary<FileStorageId, string> { [_exchange.Id] = "Exchange", [_backups.Id] = "Backups" },
            new Dictionary<SmartSchemaId, string> { [_reduce.Id] = "Reduce", [_keep.Id] = "Keep" },
            new Dictionary<ApiClientId, string> { [_packages.Id] = "Bagetter" });

        Assert.Equal("ltgmz", model.ServiceDescriptionSignature);
        Assert.Equal(".up!", model.UploadTempExtension);
        Assert.Equal("yyyyMMddHHmmss", model.ProgramArchiveDateMask);
        Assert.Equal(".zip", model.ProgramArchiveExtension);
        Assert.Equal("yyyyMMdd", model.ParametersFileDateMask);
        Assert.Equal(".json", model.ParametersFileExtension);
        Assert.Equal(TestData.MadeUpLicenseKey, model.MediatRLicenseKey);
        Assert.Equal("Exchange", model.FileStorageNameForExchange);
        Assert.Equal("Reduce", model.SmartSchemaNameForExchange);
        Assert.Equal("Keep", model.SmartSchemaNameForLocal);
        Assert.Equal("Bagetter", model.LocalPackageManagerWebApiClientName);
        Assert.Equal(".down!", model.DatabasesBackupFilesExchange.DownloadTempExtension);
        Assert.Equal(".up!", model.DatabasesBackupFilesExchange.UploadTempExtension);
        Assert.Equal("Backups", model.DatabasesBackupFilesExchange.ExchangeFileStorageName);
        Assert.Equal("Reduce", model.DatabasesBackupFilesExchange.ExchangeSmartSchemaName);
        Assert.Equal("Keep", model.DatabasesBackupFilesExchange.LocalSmartSchemaName);
        Assert.Equal(7, model.Version);
    }

    //Before the singleton is created nothing uses a record
    [Fact]
    public void GetUsages_IsEmpty_BeforeTheSingletonIsCreated()
    {
        GlobalSettings? globalSettings = null;

        Assert.Empty(globalSettings.GetUsages(_exchange.Id));
        Assert.Empty(globalSettings.GetUsages(_reduce.Id));
        Assert.Empty(globalSettings.GetUsages(_packages.Id));
    }
}
