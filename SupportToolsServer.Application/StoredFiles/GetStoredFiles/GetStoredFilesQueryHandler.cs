using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.StoredFiles;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.StoredFiles.GetStoredFiles;

//ფაილების სია მხოლოდ მეტამონაცემებია, გზით დალაგებული: შიგთავსი ბაზიდანაც არ იკითხება
public sealed class GetStoredFilesQueryHandler : IQueryHandler<GetStoredFilesQuery, List<StsStoredFileInfoDataModel>>
{
    private readonly IStoredFileRepository _storedFileRepository;

    public GetStoredFilesQueryHandler(IStoredFileRepository storedFileRepository)
    {
        _storedFileRepository = storedFileRepository;
    }

    public async Task<Result<List<StsStoredFileInfoDataModel>>> Handle(GetStoredFilesQuery query,
        CancellationToken cancellationToken)
    {
        List<StoredFileInfo> storedFileInfos = await _storedFileRepository.GetInfos(cancellationToken);

        List<StsStoredFileInfoDataModel> storedFileInfoModels =
        [
            .. storedFileInfos.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel())
        ];
        return storedFileInfoModels;
    }
}
