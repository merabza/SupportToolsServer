using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.FileStorages;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.FileStorages.DeleteFileStorage;

public sealed class DeleteFileStorageCommandHandler : ICommandHandler<DeleteFileStorageCommand>
{
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFileStorageCommandHandler(IFileStorageRepository fileStorageRepository, IUnitOfWork unitOfWork)
    {
        _fileStorageRepository = fileStorageRepository;
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

        //აქ მოწმდება, ხომ არ მიმართავს ჩანაწერს სხვა აგრეგატი (409 RecordIsInUse მომხმარებლების სიით). FileStorage-ს ჯერ
        //არავინ მიმართავს: B5 (GlobalSettings, ProjectCreatorSettings) და B6/B7 (ბაზის პარამეტრები) FK-ს Restrict-ით
        //დაამატებენ და მომხმარებლების შემოწმებას აქ ჩასვამენ

        _fileStorageRepository.Delete(fileStorage);
        return await RecordVersions.SaveChanges(_unitOfWork, FileStorageContractMapper.EntityName, command.Name,
            fileStorage.Version, async ct => (await _fileStorageRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
