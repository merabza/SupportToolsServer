using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Settings;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;

public sealed class DeleteSmartSchemaCommandHandler : ICommandHandler<DeleteSmartSchemaCommand>
{
    private readonly IGlobalSettingsRepository _globalSettingsRepository;
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly ISmartSchemaRepository _smartSchemaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSmartSchemaCommandHandler(ISmartSchemaRepository smartSchemaRepository,
        IGlobalSettingsRepository globalSettingsRepository,
        IProjectCreatorSettingsRepository projectCreatorSettingsRepository, IUnitOfWork unitOfWork)
    {
        _smartSchemaRepository = smartSchemaRepository;
        _globalSettingsRepository = globalSettingsRepository;
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
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
        //მომხმარებლების სიით. B6/B7 (ბაზის პარამეტრები) თავის მომხმარებლებს აქ დაამატებენ. დეტალები სქემასთან ერთად
        //იშლება
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

    //მომხმარებლები: ჯერ გლობალური პარამეტრების, მერე პროექტის შემქმნელის პარამეტრების ველები, კონტრაქტის რიგით
    private async Task<List<string>> GetUsages(SmartSchemaId smartSchemaId, CancellationToken cancellationToken)
    {
        GlobalSettings? globalSettings = await _globalSettingsRepository.Get(cancellationToken);
        ProjectCreatorSettings? projectCreatorSettings =
            await _projectCreatorSettingsRepository.Get(cancellationToken);

        return [.. globalSettings.GetUsages(smartSchemaId), .. projectCreatorSettings.GetUsages(smartSchemaId)];
    }
}
