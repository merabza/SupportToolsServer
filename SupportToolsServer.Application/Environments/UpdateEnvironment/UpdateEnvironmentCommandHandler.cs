using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Environments.UpdateEnvironment;

public sealed class UpdateEnvironmentCommandHandler : ICommandHandler<UpdateEnvironmentCommand, int>
{
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEnvironmentCommandHandler(IDeploymentEnvironmentRepository environmentRepository,
        IUnitOfWork unitOfWork)
    {
        _environmentRepository = environmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateEnvironmentCommand command, CancellationToken cancellationToken)
    {
        StsEnvironmentDataModel model = command.Environment;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        DeploymentEnvironment? stored = await _environmentRepository.GetByName(model.Name, cancellationToken);
        Result versionResult =
            RecordVersions.Check(EnvironmentContractMapper.EntityName, model.Name, model.Version, stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        DeploymentEnvironment environment;
        if (stored is null)
        {
            environment = DeploymentEnvironment.Create(model.Name, model.Description);
            _environmentRepository.Add(environment);
        }
        else
        {
            stored.Update(model.Name, model.Description);
            _environmentRepository.Update(stored);
            environment = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, EnvironmentContractMapper.EntityName,
            model.Name, model.Version,
            async ct => (await _environmentRepository.GetByName(model.Name, ct))?.Version, cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return environment.Version;
    }
}
