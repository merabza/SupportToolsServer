using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Runtimes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Runtimes.GetRuntimes;

public sealed class GetRuntimesQueryHandler : IQueryHandler<GetRuntimesQuery, List<StsRuntimeDataModel>>
{
    private readonly IRuntimeRepository _runtimeRepository;

    public GetRuntimesQueryHandler(IRuntimeRepository runtimeRepository)
    {
        _runtimeRepository = runtimeRepository;
    }

    public async Task<Result<List<StsRuntimeDataModel>>> Handle(GetRuntimesQuery query,
        CancellationToken cancellationToken)
    {
        List<Runtime> runtimes = await _runtimeRepository.GetAll(cancellationToken);

        List<StsRuntimeDataModel> runtimeModels =
        [
            .. runtimes.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel())
        ];
        return runtimeModels;
    }
}
