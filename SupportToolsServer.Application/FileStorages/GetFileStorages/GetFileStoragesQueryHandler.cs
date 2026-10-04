using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.FileStorages;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.FileStorages.GetFileStorages;

public sealed class GetFileStoragesQueryHandler : IQueryHandler<GetFileStoragesQuery, List<StsFileStorageDataModel>>
{
    private readonly IFileStorageRepository _fileStorageRepository;

    public GetFileStoragesQueryHandler(IFileStorageRepository fileStorageRepository)
    {
        _fileStorageRepository = fileStorageRepository;
    }

    public async Task<Result<List<StsFileStorageDataModel>>> Handle(GetFileStoragesQuery query,
        CancellationToken cancellationToken)
    {
        List<FileStorage> fileStorages = await _fileStorageRepository.GetAll(cancellationToken);

        List<StsFileStorageDataModel> fileStorageModels =
        [
            .. fileStorages.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel())
        ];
        return fileStorageModels;
    }
}
