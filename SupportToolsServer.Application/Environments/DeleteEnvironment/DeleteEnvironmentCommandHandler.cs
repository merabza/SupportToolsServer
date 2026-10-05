using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Settings;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Environments.DeleteEnvironment;

public sealed class DeleteEnvironmentCommandHandler : ICommandHandler<DeleteEnvironmentCommand>
{
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEnvironmentCommandHandler(IDeploymentEnvironmentRepository environmentRepository,
        IProjectCreatorSettingsRepository projectCreatorSettingsRepository, IUnitOfWork unitOfWork)
    {
        _environmentRepository = environmentRepository;
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
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

        //Environment-ს სხვა აგრეგატი მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse
        //მომხმარებლების სიით. B7 (ServerInfo) თავის მომხმარებლებს აქ დაამატებს
        List<string> usages = await GetUsages(environment.Id, cancellationToken);
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(EnvironmentContractMapper.EntityName, command.Name,
                usages);
        }

        _environmentRepository.Delete(environment);
        return await RecordVersions.SaveChanges(_unitOfWork, EnvironmentContractMapper.EntityName, command.Name,
            environment.Version, async ct => (await _environmentRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }

    //მომხმარებლები: პროექტის შემქმნელის პარამეტრების ველი ("ProjectCreatorSettings.<ველი>")
    private async Task<List<string>> GetUsages(DeploymentEnvironmentId environmentId,
        CancellationToken cancellationToken)
    {
        ProjectCreatorSettings? projectCreatorSettings =
            await _projectCreatorSettingsRepository.Get(cancellationToken);

        return [.. projectCreatorSettings.GetUsages(environmentId)];
    }
}
