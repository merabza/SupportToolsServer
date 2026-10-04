using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.Runtimes;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Servers.GetServers;

public sealed class GetServersQueryHandler : IQueryHandler<GetServersQuery, List<StsServerDataModel>>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IRuntimeRepository _runtimeRepository;
    private readonly IServerRepository _serverRepository;

    public GetServersQueryHandler(IServerRepository serverRepository, IApiClientRepository apiClientRepository,
        IRuntimeRepository runtimeRepository)
    {
        _serverRepository = serverRepository;
        _apiClientRepository = apiClientRepository;
        _runtimeRepository = runtimeRepository;
    }

    public async Task<Result<List<StsServerDataModel>>> Handle(GetServersQuery query,
        CancellationToken cancellationToken)
    {
        List<Server> servers = await _serverRepository.GetAll(cancellationToken);
        Dictionary<ApiClientId, string> apiClientNames =
            (await _apiClientRepository.GetAll(cancellationToken)).ToNamesById();
        Dictionary<RuntimeId, string> runtimeNames = (await _runtimeRepository.GetAll(cancellationToken)).ToNamesById();

        List<StsServerDataModel> serverModels =
        [
            .. servers.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.ToContractModel(apiClientNames, runtimeNames))
        ];
        return serverModels;
    }
}
