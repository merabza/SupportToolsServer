using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.FileStorages;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.SmartSchemas;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Settings.UpdateGlobalSettings;

public sealed class UpdateGlobalSettingsCommandHandler : ICommandHandler<UpdateGlobalSettingsCommand, int>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IGlobalSettingsRepository _globalSettingsRepository;
    private readonly ISmartSchemaRepository _smartSchemaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGlobalSettingsCommandHandler(IGlobalSettingsRepository globalSettingsRepository,
        IFileStorageRepository fileStorageRepository, ISmartSchemaRepository smartSchemaRepository,
        IApiClientRepository apiClientRepository, IUnitOfWork unitOfWork)
    {
        _globalSettingsRepository = globalSettingsRepository;
        _fileStorageRepository = fileStorageRepository;
        _smartSchemaRepository = smartSchemaRepository;
        _apiClientRepository = apiClientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateGlobalSettingsCommand command, CancellationToken cancellationToken)
    {
        StsGlobalSettingsDataModel model = command.GlobalSettings;

        //Version 0 — პირველი შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        GlobalSettings? stored = await _globalSettingsRepository.Get(cancellationToken);
        Result versionResult = RecordVersions.Check(GlobalSettingsContractMapper.EntityName,
            GlobalSettingsContractMapper.RecordName, model.Version, stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        //მითითებები სახელით მოდის, ბაზაში კი მათი Id-ები ინახება. ცარიელი სახელი ნიშნავს, რომ მითითება არ არის. ყველა
        //არარსებული სახელი ერთ შეცდომაში ბრუნდება, ჩანაწერის ტიპებად დაჯგუფებული
        StsDatabasesBackupFilesExchangeDataModel exchangeModel = model.DatabasesBackupFilesExchange;
        var references = new ReferencedRecords();
        FileStorage? fileStorageForExchange = await references.Find(model.FileStorageNameForExchange,
            FileStorageContractMapper.EntityName, _fileStorageRepository.GetByName, cancellationToken);
        SmartSchema? smartSchemaForExchange = await references.Find(model.SmartSchemaNameForExchange,
            SmartSchemaContractMapper.EntityName, _smartSchemaRepository.GetByName, cancellationToken);
        SmartSchema? smartSchemaForLocal = await references.Find(model.SmartSchemaNameForLocal,
            SmartSchemaContractMapper.EntityName, _smartSchemaRepository.GetByName, cancellationToken);
        ApiClient? localPackageManagerWebApiClient = await references.Find(model.LocalPackageManagerWebApiClientName,
            ApiClientContractMapper.EntityName, _apiClientRepository.GetByName, cancellationToken);
        FileStorage? exchangeFileStorage = await references.Find(exchangeModel.ExchangeFileStorageName,
            FileStorageContractMapper.EntityName, _fileStorageRepository.GetByName, cancellationToken);
        SmartSchema? exchangeSmartSchema = await references.Find(exchangeModel.ExchangeSmartSchemaName,
            SmartSchemaContractMapper.EntityName, _smartSchemaRepository.GetByName, cancellationToken);
        SmartSchema? localSmartSchema = await references.Find(exchangeModel.LocalSmartSchemaName,
            SmartSchemaContractMapper.EntityName, _smartSchemaRepository.GetByName, cancellationToken);
        if (!references.AreAllFound)
        {
            return references.MissingError();
        }

        var exchange = new DatabasesBackupFilesExchange(exchangeModel.DownloadTempExtension,
            exchangeModel.UploadTempExtension, exchangeFileStorage?.Id, exchangeSmartSchema?.Id, localSmartSchema?.Id);
        GlobalSettings globalSettings;
        if (stored is null)
        {
            globalSettings = GlobalSettings.Create(model.ServiceDescriptionSignature, model.UploadTempExtension,
                model.ProgramArchiveDateMask, model.ProgramArchiveExtension, model.ParametersFileDateMask,
                model.ParametersFileExtension, model.MediatRLicenseKey, fileStorageForExchange?.Id,
                smartSchemaForExchange?.Id, smartSchemaForLocal?.Id, localPackageManagerWebApiClient?.Id, exchange);
            _globalSettingsRepository.Add(globalSettings);
        }
        else
        {
            stored.Update(model.ServiceDescriptionSignature, model.UploadTempExtension, model.ProgramArchiveDateMask,
                model.ProgramArchiveExtension, model.ParametersFileDateMask, model.ParametersFileExtension,
                model.MediatRLicenseKey, fileStorageForExchange?.Id, smartSchemaForExchange?.Id,
                smartSchemaForLocal?.Id, localPackageManagerWebApiClient?.Id, exchange);
            _globalSettingsRepository.Update(stored);
            globalSettings = stored;
        }

        //ორი ერთდროული პირველი შექმნიდან მეორე INSERT-ს ფიქსირებული გასაღების PK აჩერებს, განახლებას კი Version-ის
        //token-ი: ორივე შემთხვევაში ვერსია თავიდან იკითხება და 409 ConcurrencyConflict ბრუნდება
        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, GlobalSettingsContractMapper.EntityName,
            GlobalSettingsContractMapper.RecordName, model.Version,
            async ct => (await _globalSettingsRepository.Get(ct))?.Version, cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return globalSettings.Version;
    }
}
