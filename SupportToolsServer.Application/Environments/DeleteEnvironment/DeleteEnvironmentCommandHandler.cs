using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Environments.DeleteEnvironment;

public sealed class DeleteEnvironmentCommandHandler : ICommandHandler<DeleteEnvironmentCommand>
{
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEnvironmentCommandHandler(IDeploymentEnvironmentRepository environmentRepository,
        IUnitOfWork unitOfWork)
    {
        _environmentRepository = environmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteEnvironmentCommand command, CancellationToken cancellationToken)
    {
        DeploymentEnvironment? environment = await _environmentRepository.GetByName(command.Name, cancellationToken);
        if (environment is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(EnvironmentContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != environment.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(EnvironmentContractMapper.EntityName,
                command.Name, command.Version.Value, environment.Version);
        }

        //აქ მოწმდება, ხომ არ მიმართავს ჩანაწერს სხვა აგრეგატი (409 RecordIsInUse მომხმარებლების სიით). Environment-ს
        //ჯერ არავინ მიმართავს: B5 (ProjectCreatorSettings) და B7 (ServerInfo) FK-ს Restrict-ით დაამატებენ და
        //მომხმარებლების შემოწმებას აქ ჩასვამენ

        _environmentRepository.Delete(environment);
        return await RecordVersions.SaveChanges(_unitOfWork, EnvironmentContractMapper.EntityName, command.Name,
            environment.Version, async ct => (await _environmentRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
