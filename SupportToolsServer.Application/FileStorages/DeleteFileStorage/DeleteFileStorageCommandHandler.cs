using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Settings;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.FileStorages.DeleteFileStorage;

public sealed class DeleteFileStorageCommandHandler : ICommandHandler<DeleteFileStorageCommand>
{
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IGlobalSettingsRepository _globalSettingsRepository;
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFileStorageCommandHandler(IFileStorageRepository fileStorageRepository,
        IGlobalSettingsRepository globalSettingsRepository,
        IProjectCreatorSettingsRepository projectCreatorSettingsRepository, IUnitOfWork unitOfWork)
    {
        _fileStorageRepository = fileStorageRepository;
        _globalSettingsRepository = globalSettingsRepository;
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteFileStorageCommand command, CancellationToken cancellationToken)
    {
        FileStorage? fileStorage = await _fileStorageRepository.GetByName(command.Name, cancellationToken);
        if (fileStorage is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(FileStorageContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != fileStorage.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(FileStorageContractMapper.EntityName,
                command.Name, command.Version.Value, fileStorage.Version);
        }

        //FileStorage-ს სხვა აგრეგატები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse
        //მომხმარებლების სიით. B6/B7 (ბაზის პარამეტრები) თავის მომხმარებლებს აქ დაამატებენ
        List<string> usages = await GetUsages(fileStorage.Id, cancellationToken);
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(FileStorageContractMapper.EntityName, command.Name,
                usages);
        }

        _fileStorageRepository.Delete(fileStorage);
        return await RecordVersions.SaveChanges(_unitOfWork, FileStorageContractMapper.EntityName, command.Name,
            fileStorage.Version, async ct => (await _fileStorageRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }

    //მომხმარებლები: ჯერ გლობალური პარამეტრების, მერე პროექტის შემქმნელის პარამეტრების ველები, კონტრაქტის რიგით
    private async Task<List<string>> GetUsages(FileStorageId fileStorageId, CancellationToken cancellationToken)
    {
        GlobalSettings? globalSettings = await _globalSettingsRepository.Get(cancellationToken);
        ProjectCreatorSettings? projectCreatorSettings =
            await _projectCreatorSettingsRepository.Get(cancellationToken);

        return [.. globalSettings.GetUsages(fileStorageId), .. projectCreatorSettings.GetUsages(fileStorageId)];
    }
}
