using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnectionByName;

public sealed class GetDatabaseServerConnectionByNameQueryHandler : IQueryHandler<
    GetDatabaseServerConnectionByNameQuery, StsDatabaseServerConnectionDataModel>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;

    public GetDatabaseServerConnectionByNameQueryHandler(
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        IApiClientRepository apiClientRepository)
    {
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _apiClientRepository = apiClientRepository;
    }

    public async Task<Result<StsDatabaseServerConnectionDataModel>> Handle(
        GetDatabaseServerConnectionByNameQuery query, CancellationToken cancellationToken)
    {
        DatabaseServerConnection? connection =
            await _databaseServerConnectionRepository.GetByName(query.Name, cancellationToken);
        if (connection is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(
                DatabaseServerConnectionContractMapper.EntityName, query.Name);
        }

        return connection.ToContractModel((await _apiClientRepository.GetAll(cancellationToken)).ToNamesById());
    }
}
