using System;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.StoredFiles;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.StoredFiles.UpdateStoredFile;

public sealed class UpdateStoredFileCommandHandler : ICommandHandler<UpdateStoredFileCommand, int>
{
    private readonly IStoredFileRepository _storedFileRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStoredFileCommandHandler(IStoredFileRepository storedFileRepository, TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _storedFileRepository = storedFileRepository;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateStoredFileCommand command, CancellationToken cancellationToken)
    {
        StsStoredFileDataModel model = command.StoredFile;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        StoredFile? stored = await _storedFileRepository.GetByPath(model.Path, cancellationToken);
        Result versionResult = RecordVersions.Check(StoredFileContractMapper.EntityName, model.Path, model.Version,
            stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        //Sha256-სა და Length-ს დომენი შიგთავსიდან ითვლის
        DateTime updatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        StoredFile storedFile;
        if (stored is null)
        {
            storedFile = StoredFile.Create(model.Path, model.Content, updatedAtUtc);
            _storedFileRepository.Add(storedFile);
        }
        else
        {
            stored.Update(model.Path, model.Content, updatedAtUtc);
            _storedFileRepository.Update(stored);
            storedFile = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, StoredFileContractMapper.EntityName,
            model.Path, model.Version,
            async ct => (await _storedFileRepository.GetByPath(model.Path, ct))?.Version, cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return storedFile.Version;
    }
}
