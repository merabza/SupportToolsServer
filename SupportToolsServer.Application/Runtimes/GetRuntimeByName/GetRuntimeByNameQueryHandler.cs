using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Runtimes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Runtimes.GetRuntimeByName;

public sealed class GetRuntimeByNameQueryHandler : IQueryHandler<GetRuntimeByNameQuery, StsRuntimeDataModel>
{
    private readonly IRuntimeRepository _runtimeRepository;

    public GetRuntimeByNameQueryHandler(IRuntimeRepository runtimeRepository)
    {
        _runtimeRepository = runtimeRepository;
    }

    public async Task<Result<StsRuntimeDataModel>> Handle(GetRuntimeByNameQuery query,
        CancellationToken cancellationToken)
    {
        Runtime? runtime = await _runtimeRepository.GetByName(query.Name, cancellationToken);
        if (runtime is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(RuntimeContractMapper.EntityName,
                query.Name);
        }

        return runtime.ToContractModel();
    }
}
