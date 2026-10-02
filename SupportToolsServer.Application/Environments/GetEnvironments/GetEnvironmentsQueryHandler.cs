using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Environments.GetEnvironments;

public sealed class GetEnvironmentsQueryHandler : IQueryHandler<GetEnvironmentsQuery, List<StsEnvironmentDataModel>>
{
    private readonly IDeploymentEnvironmentRepository _environmentRepository;

    public GetEnvironmentsQueryHandler(IDeploymentEnvironmentRepository environmentRepository)
    {
        _environmentRepository = environmentRepository;
    }

    public async Task<Result<List<StsEnvironmentDataModel>>> Handle(GetEnvironmentsQuery query,
        CancellationToken cancellationToken)
    {
        List<DeploymentEnvironment> environments = await _environmentRepository.GetAll(cancellationToken);

        List<StsEnvironmentDataModel> environmentModels =
        [
            .. environments.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel())
        ];
        return environmentModels;
    }
}
