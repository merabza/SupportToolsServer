using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.StoredFiles;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.StoredFiles.GetStoredFileByPath;

public sealed class GetStoredFileByPathQueryHandler : IQueryHandler<GetStoredFileByPathQuery, StsStoredFileDataModel>
{
    private readonly IStoredFileRepository _storedFileRepository;

    public GetStoredFileByPathQueryHandler(IStoredFileRepository storedFileRepository)
    {
        _storedFileRepository = storedFileRepository;
    }

    public async Task<Result<StsStoredFileDataModel>> Handle(GetStoredFileByPathQuery query,
        CancellationToken cancellationToken)
    {
        StoredFile? storedFile = await _storedFileRepository.GetByPath(query.Path, cancellationToken);
        if (storedFile is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(StoredFileContractMapper.EntityName,
                query.Path);
        }

        return storedFile.ToContractModel();
    }
}
