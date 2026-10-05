using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.DatabaseServerConnections;
using SupportToolsServer.Application.Environments;
using SupportToolsServer.Application.FileStorages;
using SupportToolsServer.Application.Registry;
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
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Settings.UpdateProjectCreatorSettings;

public sealed class
    UpdateProjectCreatorSettingsCommandHandler : ICommandHandler<UpdateProjectCreatorSettingsCommand, int>
{
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly IServerRepository _serverRepository;
    private readonly ISmartSchemaRepository _smartSchemaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCreatorSettingsCommandHandler(
        IProjectCreatorSettingsRepository projectCreatorSettingsRepository, IServerRepository serverRepository,
        IDeploymentEnvironmentRepository environmentRepository,
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        IFileStorageRepository fileStorageRepository, ISmartSchemaRepository smartSchemaRepository,
        IUnitOfWork unitOfWork)
    {
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
        _serverRepository = serverRepository;
        _environmentRepository = environmentRepository;
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _fileStorageRepository = fileStorageRepository;
        _smartSchemaRepository = smartSchemaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateProjectCreatorSettingsCommand command,
        CancellationToken cancellationToken)
    {
        StsProjectCreatorSettingsDataModel model = command.ProjectCreatorSettings;

        //Version 0 — პირველი შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        ProjectCreatorSettings? stored = await _projectCreatorSettingsRepository.Get(cancellationToken);
        Result versionResult = RecordVersions.Check(ProjectCreatorSettingsContractMapper.EntityName,
            ProjectCreatorSettingsContractMapper.RecordName, model.Version, stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        //მითითებები სახელით მოდის, ბაზაში კი მათი Id-ები ინახება. ცარიელი სახელი ნიშნავს, რომ მითითება არ არის. ყველა
        //არარსებული სახელი ერთ შეცდომაში ბრუნდება, ჩანაწერის ტიპებად დაჯგუფებული
        var references = new ReferencedRecords();
        Server? productionServer = await references.Find(model.ProductionServerName, ServerContractMapper.EntityName,
            _serverRepository.GetByName, cancellationToken);
        DeploymentEnvironment? productionEnvironment = await references.Find(model.ProductionEnvironmentName,
            EnvironmentContractMapper.EntityName, _environmentRepository.GetByName, cancellationToken);
        DatabaseServerConnection? developerDbConnection = await references.Find(model.DeveloperDbConnectionName,
            DatabaseServerConnectionContractMapper.EntityName, _databaseServerConnectionRepository.GetByName,
            cancellationToken);
        FileStorage? databaseExchangeFileStorage = await references.Find(model.DatabaseExchangeFileStorageName,
            FileStorageContractMapper.EntityName, _fileStorageRepository.GetByName, cancellationToken);
        SmartSchema? useSmartSchema = await references.Find(model.UseSmartSchema,
            SmartSchemaContractMapper.EntityName, _smartSchemaRepository.GetByName, cancellationToken);
        if (!references.AreAllFound)
        {
            return references.MissingError();
        }

        ProjectCreatorSettings projectCreatorSettings;
        if (stored is null)
        {
            projectCreatorSettings = ProjectCreatorSettings.Create(model.IndentSize, model.FakeHostProjectName,
                model.ProjectsFolderPathReal, model.SecretsFolderPathReal, productionServer?.Id,
                productionEnvironment?.Id, developerDbConnection?.Id, databaseExchangeFileStorage?.Id,
                useSmartSchema?.Id);
            _projectCreatorSettingsRepository.Add(projectCreatorSettings);
        }
        else
        {
            stored.Update(model.IndentSize, model.FakeHostProjectName, model.ProjectsFolderPathReal,
                model.SecretsFolderPathReal, productionServer?.Id, productionEnvironment?.Id,
                developerDbConnection?.Id, databaseExchangeFileStorage?.Id, useSmartSchema?.Id);
            _projectCreatorSettingsRepository.Update(stored);
            projectCreatorSettings = stored;
        }

        //ორი ერთდროული პირველი შექმნიდან მეორე INSERT-ს ფიქსირებული გასაღების PK აჩერებს, განახლებას კი Version-ის
        //token-ი: ორივე შემთხვევაში ვერსია თავიდან იკითხება და 409 ConcurrencyConflict ბრუნდება
        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork,
            ProjectCreatorSettingsContractMapper.EntityName, ProjectCreatorSettingsContractMapper.RecordName,
            model.Version, async ct => (await _projectCreatorSettingsRepository.Get(ct))?.Version, cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return projectCreatorSettings.Version;
    }
}
