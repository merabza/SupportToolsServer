using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Environments.GetEnvironmentByName;

public sealed class GetEnvironmentByNameQueryHandler : IQueryHandler<GetEnvironmentByNameQuery, StsEnvironmentDataModel>
{
    private readonly IDeploymentEnvironmentRepository _environmentRepository;

    public GetEnvironmentByNameQueryHandler(IDeploymentEnvironmentRepository environmentRepository)
    {
        _environmentRepository = environmentRepository;
    }

    public async Task<Result<StsEnvironmentDataModel>> Handle(GetEnvironmentByNameQuery query,
        CancellationToken cancellationToken)
    {
        DeploymentEnvironment? environment = await _environmentRepository.GetByName(query.Name, cancellationToken);
        if (environment is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(EnvironmentContractMapper.EntityName,
                query.Name);
        }

        return environment.ToContractModel();
    }
}
