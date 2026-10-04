using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.FileStorages;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.FileStorages.UpdateFileStorage;

public sealed class UpdateFileStorageCommandHandler : ICommandHandler<UpdateFileStorageCommand, int>
{
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFileStorageCommandHandler(IFileStorageRepository fileStorageRepository, IUnitOfWork unitOfWork)
    {
        _fileStorageRepository = fileStorageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateFileStorageCommand command, CancellationToken cancellationToken)
    {
        StsFileStorageDataModel model = command.FileStorage;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        FileStorage? stored = await _fileStorageRepository.GetByName(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(FileStorageContractMapper.EntityName, model.Name, model.Version,
            stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        FileStorage fileStorage;
        if (stored is null)
        {
            fileStorage = FileStorage.Create(model.Name, model.FileStoragePath, model.UserName, model.Password,
                model.FileNameMaxLength, model.FileSizeSplitPositionInRow, model.FtpSiteLsFileOffset);
            _fileStorageRepository.Add(fileStorage);
        }
        else
        {
            stored.Update(model.Name, model.FileStoragePath, model.UserName, model.Password, model.FileNameMaxLength,
                model.FileSizeSplitPositionInRow, model.FtpSiteLsFileOffset);
            _fileStorageRepository.Update(stored);
            fileStorage = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, FileStorageContractMapper.EntityName,
            model.Name, model.Version, async ct => (await _fileStorageRepository.GetByName(model.Name, ct))?.Version,
            cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return fileStorage.Version;
    }
}
