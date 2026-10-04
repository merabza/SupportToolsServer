using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.Runtimes;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Servers.GetServerByName;

public sealed class GetServerByNameQueryHandler : IQueryHandler<GetServerByNameQuery, StsServerDataModel>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IRuntimeRepository _runtimeRepository;
    private readonly IServerRepository _serverRepository;

    public GetServerByNameQueryHandler(IServerRepository serverRepository, IApiClientRepository apiClientRepository,
        IRuntimeRepository runtimeRepository)
    {
        _serverRepository = serverRepository;
        _apiClientRepository = apiClientRepository;
        _runtimeRepository = runtimeRepository;
    }

    public async Task<Result<StsServerDataModel>> Handle(GetServerByNameQuery query,
        CancellationToken cancellationToken)
    {
        Server? server = await _serverRepository.GetByName(query.Name, cancellationToken);
        if (server is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ServerContractMapper.EntityName,
                query.Name);
        }

        return server.ToContractModel((await _apiClientRepository.GetAll(cancellationToken)).ToNamesById(),
            (await _runtimeRepository.GetAll(cancellationToken)).ToNamesById());
    }
}
