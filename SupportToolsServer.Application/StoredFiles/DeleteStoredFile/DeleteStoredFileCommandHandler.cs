using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.StoredFiles;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.StoredFiles.DeleteStoredFile;

//ფაილს სხვა აგრეგატი FK-ით არ მიმართავს (რეესტრის გზის ველები მას მხოლოდ გზით ასახელებენ), ამიტომ გამოყენების
//შემოწმება არ არის: წაშლის გადაწყვეტილება კლიენტისაა (C6)
public sealed class DeleteStoredFileCommandHandler : ICommandHandler<DeleteStoredFileCommand>
{
    private readonly IStoredFileRepository _storedFileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteStoredFileCommandHandler(IStoredFileRepository storedFileRepository, IUnitOfWork unitOfWork)
    {
        _storedFileRepository = storedFileRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteStoredFileCommand command, CancellationToken cancellationToken)
    {
        StoredFile? storedFile = await _storedFileRepository.GetByPath(command.Path, cancellationToken);
        if (storedFile is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(StoredFileContractMapper.EntityName,
                command.Path);
        }

        if (command.Version is not null && command.Version.Value != storedFile.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(StoredFileContractMapper.EntityName,
                command.Path, command.Version.Value, storedFile.Version);
        }

        _storedFileRepository.Delete(storedFile);
        return await RecordVersions.SaveChanges(_unitOfWork, StoredFileContractMapper.EntityName, command.Path,
            storedFile.Version, async ct => (await _storedFileRepository.GetByPath(command.Path, ct))?.Version,
            cancellationToken);
    }
}
