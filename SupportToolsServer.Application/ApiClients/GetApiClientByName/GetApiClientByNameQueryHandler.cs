using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ApiClients.GetApiClientByName;

public sealed class GetApiClientByNameQueryHandler : IQueryHandler<GetApiClientByNameQuery, StsApiClientDataModel>
{
    private readonly IApiClientRepository _apiClientRepository;

    public GetApiClientByNameQueryHandler(IApiClientRepository apiClientRepository)
    {
        _apiClientRepository = apiClientRepository;
    }

    public async Task<Result<StsApiClientDataModel>> Handle(GetApiClientByNameQuery query,
        CancellationToken cancellationToken)
    {
        ApiClient? apiClient = await _apiClientRepository.GetByName(query.Name, cancellationToken);
        if (apiClient is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ApiClientContractMapper.EntityName,
                query.Name);
        }

        return apiClient.ToContractModel();
    }
}
