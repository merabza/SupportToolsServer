using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.FileStorages;
using SupportToolsServer.Application.SmartSchemas;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Settings.GetGlobalSettings;

public sealed class GetGlobalSettingsQueryHandler : IQueryHandler<GetGlobalSettingsQuery, StsGlobalSettingsDataModel>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IGlobalSettingsRepository _globalSettingsRepository;
    private readonly ISmartSchemaRepository _smartSchemaRepository;

    public GetGlobalSettingsQueryHandler(IGlobalSettingsRepository globalSettingsRepository,
        IFileStorageRepository fileStorageRepository, ISmartSchemaRepository smartSchemaRepository,
        IApiClientRepository apiClientRepository)
    {
        _globalSettingsRepository = globalSettingsRepository;
        _fileStorageRepository = fileStorageRepository;
        _smartSchemaRepository = smartSchemaRepository;
        _apiClientRepository = apiClientRepository;
    }

    //სანამ ჩანაწერი შეიქმნება, ბრუნდება ცარიელი კონტრაქტი Version = 0-ით და არა 404: კლიენტის სინქრონიზაცია ამას
    //"სერვერზე ჩანაწერი არ არის"-ად კითხულობს შეცდომის გარეშე (CLAUDE.md, Registry conventions)
    public async Task<Result<StsGlobalSettingsDataModel>> Handle(GetGlobalSettingsQuery query,
        CancellationToken cancellationToken)
    {
        GlobalSettings? globalSettings = await _globalSettingsRepository.Get(cancellationToken);
        if (globalSettings is null)
        {
            return new StsGlobalSettingsDataModel();
        }

        return globalSettings.ToContractModel((await _fileStorageRepository.GetAll(cancellationToken)).ToNamesById(),
            (await _smartSchemaRepository.GetAll(cancellationToken)).ToNamesById(),
            (await _apiClientRepository.GetAll(cancellationToken)).ToNamesById());
    }
}
