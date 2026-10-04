using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ApiClients.GetApiClients;

public sealed class GetApiClientsQueryHandler : IQueryHandler<GetApiClientsQuery, List<StsApiClientDataModel>>
{
    private readonly IApiClientRepository _apiClientRepository;

    public GetApiClientsQueryHandler(IApiClientRepository apiClientRepository)
    {
        _apiClientRepository = apiClientRepository;
    }

    public async Task<Result<List<StsApiClientDataModel>>> Handle(GetApiClientsQuery query,
        CancellationToken cancellationToken)
    {
        List<ApiClient> apiClients = await _apiClientRepository.GetAll(cancellationToken);

        List<StsApiClientDataModel> apiClientModels =
        [
            .. apiClients.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel())
        ];
        return apiClientModels;
    }
}
