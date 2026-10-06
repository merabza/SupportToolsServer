using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Projects;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Settings;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;

public sealed class DeleteSmartSchemaCommandHandler : ICommandHandler<DeleteSmartSchemaCommand>
{
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IGlobalSettingsRepository _globalSettingsRepository;
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IServerRepository _serverRepository;
    private readonly ISmartSchemaRepository _smartSchemaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSmartSchemaCommandHandler(ISmartSchemaRepository smartSchemaRepository,
        IGlobalSettingsRepository globalSettingsRepository,
        IProjectCreatorSettingsRepository projectCreatorSettingsRepository, IProjectRepository projectRepository,
        IServerRepository serverRepository, IDeploymentEnvironmentRepository environmentRepository,
        IUnitOfWork unitOfWork)
    {
        _smartSchemaRepository = smartSchemaRepository;
        _globalSettingsRepository = globalSettingsRepository;
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
        _projectRepository = projectRepository;
        _serverRepository = serverRepository;
        _environmentRepository = environmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteSmartSchemaCommand command, CancellationToken cancellationToken)
    {
        SmartSchema? smartSchema = await _smartSchemaRepository.GetByName(command.Name, cancellationToken);
        if (smartSchema is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(SmartSchemaContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != smartSchema.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(SmartSchemaContractMapper.EntityName,
                command.Name, command.Version.Value, smartSchema.Version);
        }

        //SmartSchema-ს სხვა აგრეგატები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse
        //მომხმარებლების სიით. დეტალები სქემასთან ერთად იშლება
        List<string> usages = await GetUsages(smartSchema.Id, cancellationToken);
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(SmartSchemaContractMapper.EntityName, command.Name,
                usages);
        }

        _smartSchemaRepository.Delete(smartSchema);
        return await RecordVersions.SaveChanges(_unitOfWork, SmartSchemaContractMapper.EntityName, command.Name,
            smartSchema.Version, async ct => (await _smartSchemaRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }

    //მომხმარებლები: ჯერ გლობალური პარამეტრების, მერე პროექტის შემქმნელის პარამეტრების ველები, კონტრაქტის რიგით, და
    //ბოლოს პროექტები, რომელთა ბაზის პარამეტრებიც სქემას იყენებს, თითოეული თავისი ServerInfo-ებით, რომელთა ბაზის
    //პარამეტრებიც სქემას იყენებს
    private async Task<List<string>> GetUsages(SmartSchemaId smartSchemaId, CancellationToken cancellationToken)
    {
        GlobalSettings? globalSettings = await _globalSettingsRepository.Get(cancellationToken);
        ProjectCreatorSettings? projectCreatorSettings =
            await _projectCreatorSettingsRepository.Get(cancellationToken);
        List<Project> projects = await _projectRepository.GetAll(cancellationToken);
        ServerInfoKeyNames keyNames =
            await ServerInfoKeyNames.Read(_serverRepository, _environmentRepository, cancellationToken);

        return
        [
            .. globalSettings.GetUsages(smartSchemaId), .. projectCreatorSettings.GetUsages(smartSchemaId),
            .. projects.GetUsages(smartSchemaId, keyNames)
        ];
    }
}
