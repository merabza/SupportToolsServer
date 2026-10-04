using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnections;

public sealed class GetDatabaseServerConnectionsQueryHandler : IQueryHandler<GetDatabaseServerConnectionsQuery,
    List<StsDatabaseServerConnectionDataModel>>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;

    public GetDatabaseServerConnectionsQueryHandler(
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        IApiClientRepository apiClientRepository)
    {
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _apiClientRepository = apiClientRepository;
    }

    public async Task<Result<List<StsDatabaseServerConnectionDataModel>>> Handle(
        GetDatabaseServerConnectionsQuery query, CancellationToken cancellationToken)
    {
        List<DatabaseServerConnection> connections =
            await _databaseServerConnectionRepository.GetAll(cancellationToken);
        Dictionary<ApiClientId, string> apiClientNames =
            (await _apiClientRepository.GetAll(cancellationToken)).ToNamesById();

        List<StsDatabaseServerConnectionDataModel> connectionModels =
        [
            .. connections.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.ToContractModel(apiClientNames))
        ];
        return connectionModels;
    }
}
