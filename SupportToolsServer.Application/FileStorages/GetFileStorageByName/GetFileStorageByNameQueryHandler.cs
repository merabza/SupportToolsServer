using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.FileStorages;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.FileStorages.GetFileStorageByName;

public sealed class GetFileStorageByNameQueryHandler : IQueryHandler<GetFileStorageByNameQuery, StsFileStorageDataModel>
{
    private readonly IFileStorageRepository _fileStorageRepository;

    public GetFileStorageByNameQueryHandler(IFileStorageRepository fileStorageRepository)
    {
        _fileStorageRepository = fileStorageRepository;
    }

    public async Task<Result<StsFileStorageDataModel>> Handle(GetFileStorageByNameQuery query,
        CancellationToken cancellationToken)
    {
        FileStorage? fileStorage = await _fileStorageRepository.GetByName(query.Name, cancellationToken);
        if (fileStorage is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(FileStorageContractMapper.EntityName,
                query.Name);
        }

        return fileStorage.ToContractModel();
    }
}
