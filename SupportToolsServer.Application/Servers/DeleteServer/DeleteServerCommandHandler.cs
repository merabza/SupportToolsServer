using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Projects;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Settings;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Servers.DeleteServer;

public sealed class DeleteServerCommandHandler : ICommandHandler<DeleteServerCommand>
{
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IServerRepository _serverRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServerCommandHandler(IServerRepository serverRepository,
        IProjectCreatorSettingsRepository projectCreatorSettingsRepository, IProjectRepository projectRepository,
        IDeploymentEnvironmentRepository environmentRepository, IUnitOfWork unitOfWork)
    {
        _serverRepository = serverRepository;
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
        _projectRepository = projectRepository;
        _environmentRepository = environmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteServerCommand command, CancellationToken cancellationToken)
    {
        Server? server = await _serverRepository.GetByName(command.Name, cancellationToken);
        if (server is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ServerContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != server.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(ServerContractMapper.EntityName, command.Name,
                command.Version.Value, server.Version);
        }

        //Server-ს სხვა აგრეგატები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse
        //მომხმარებლების სიით
        List<string> usages = await GetUsages(server.Id, cancellationToken);
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(ServerContractMapper.EntityName, command.Name,
                usages);
        }

        _serverRepository.Delete(server);
        return await RecordVersions.SaveChanges(_unitOfWork, ServerContractMapper.EntityName, command.Name,
            server.Version, async ct => (await _serverRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }

    //მომხმარებლები: პროექტის შემქმნელის პარამეტრების ველი ("ProjectCreatorSettings.<ველი>"), მერე პროექტების
    //ServerInfo-ები ("Project <სახელი> / <სერვერი>|<გარემო>")
    private async Task<List<string>> GetUsages(ServerId serverId, CancellationToken cancellationToken)
    {
        ProjectCreatorSettings? projectCreatorSettings =
            await _projectCreatorSettingsRepository.Get(cancellationToken);
        List<Project> projects = await _projectRepository.GetAll(cancellationToken);
        ServerInfoKeyNames keyNames =
            await ServerInfoKeyNames.Read(_serverRepository, _environmentRepository, cancellationToken);

        return [.. projectCreatorSettings.GetUsages(serverId), .. projects.GetUsages(serverId, keyNames)];
    }
}
