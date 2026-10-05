using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.DatabaseServerConnections;
using SupportToolsServer.Application.Environments;
using SupportToolsServer.Application.FileStorages;
using SupportToolsServer.Application.Servers;
using SupportToolsServer.Application.SmartSchemas;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Settings.GetProjectCreatorSettings;

public sealed class
    GetProjectCreatorSettingsQueryHandler : IQueryHandler<GetProjectCreatorSettingsQuery,
    StsProjectCreatorSettingsDataModel>
{
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly IServerRepository _serverRepository;
    private readonly ISmartSchemaRepository _smartSchemaRepository;

    public GetProjectCreatorSettingsQueryHandler(IProjectCreatorSettingsRepository projectCreatorSettingsRepository,
        IServerRepository serverRepository, IDeploymentEnvironmentRepository environmentRepository,
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        IFileStorageRepository fileStorageRepository, ISmartSchemaRepository smartSchemaRepository)
    {
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
        _serverRepository = serverRepository;
        _environmentRepository = environmentRepository;
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _fileStorageRepository = fileStorageRepository;
        _smartSchemaRepository = smartSchemaRepository;
    }

    //სანამ ჩანაწერი შეიქმნება, ბრუნდება ცარიელი კონტრაქტი Version = 0-ით და არა 404: კლიენტის სინქრონიზაცია ამას
    //"სერვერზე ჩანაწერი არ არის"-ად კითხულობს შეცდომის გარეშე (CLAUDE.md, Registry conventions)
    public async Task<Result<StsProjectCreatorSettingsDataModel>> Handle(GetProjectCreatorSettingsQuery query,
        CancellationToken cancellationToken)
    {
        ProjectCreatorSettings? projectCreatorSettings =
            await _projectCreatorSettingsRepository.Get(cancellationToken);
        if (projectCreatorSettings is null)
        {
            return new StsProjectCreatorSettingsDataModel();
        }

        return projectCreatorSettings.ToContractModel(
            (await _serverRepository.GetAll(cancellationToken)).ToNamesById(),
            (await _environmentRepository.GetAll(cancellationToken)).ToNamesById(),
            (await _databaseServerConnectionRepository.GetAll(cancellationToken)).ToNamesById(),
            (await _fileStorageRepository.GetAll(cancellationToken)).ToNamesById(),
            (await _smartSchemaRepository.GetAll(cancellationToken)).ToNamesById());
    }
}
