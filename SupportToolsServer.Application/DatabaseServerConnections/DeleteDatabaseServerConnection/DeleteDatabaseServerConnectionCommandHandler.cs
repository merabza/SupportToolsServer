using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Projects;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Settings;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;

public sealed class DeleteDatabaseServerConnectionCommandHandler : ICommandHandler<DeleteDatabaseServerConnectionCommand>
{
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IServerRepository _serverRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDatabaseServerConnectionCommandHandler(
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        IProjectCreatorSettingsRepository projectCreatorSettingsRepository, IProjectRepository projectRepository,
        IServerRepository serverRepository, IDeploymentEnvironmentRepository environmentRepository,
        IUnitOfWork unitOfWork)
    {
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
        _projectRepository = projectRepository;
        _serverRepository = serverRepository;
        _environmentRepository = environmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteDatabaseServerConnectionCommand command,
        CancellationToken cancellationToken)
    {
        DatabaseServerConnection? connection =
            await _databaseServerConnectionRepository.GetByName(command.Name, cancellationToken);
        if (connection is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(
                DatabaseServerConnectionContractMapper.EntityName, command.Name);
        }

        if (command.Version is not null && command.Version.Value != connection.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(
                DatabaseServerConnectionContractMapper.EntityName, command.Name, command.Version.Value,
                connection.Version);
        }

        //DatabaseServerConnection-ს სხვა აგრეგატები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409
        //RecordIsInUse მომხმარებლების სიით. folders set-ები კავშირთან ერთად იშლება
        List<string> usages = await GetUsages(connection.Id, cancellationToken);
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(DatabaseServerConnectionContractMapper.EntityName,
                command.Name, usages);
        }

        _databaseServerConnectionRepository.Delete(connection);
        return await RecordVersions.SaveChanges(_unitOfWork, DatabaseServerConnectionContractMapper.EntityName,
            command.Name, connection.Version,
            async ct => (await _databaseServerConnectionRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }

    //მომხმარებლები: პროექტის შემქმნელის პარამეტრების ველი ("ProjectCreatorSettings.<ველი>"), მერე პროექტები, რომელთა
    //ბაზის პარამეტრებიც კავშირს იყენებს ("Project <სახელი>"), თითოეული თავისი ServerInfo-ებით, რომელთა ბაზის
    //პარამეტრებიც კავშირს იყენებს ("Project <სახელი> / <სერვერი>|<გარემო>")
    private async Task<List<string>> GetUsages(DatabaseServerConnectionId connectionId,
        CancellationToken cancellationToken)
    {
        ProjectCreatorSettings? projectCreatorSettings =
            await _projectCreatorSettingsRepository.Get(cancellationToken);
        List<Project> projects = await _projectRepository.GetAll(cancellationToken);
        ServerInfoKeyNames keyNames =
            await ServerInfoKeyNames.Read(_serverRepository, _environmentRepository, cancellationToken);

        return [.. projectCreatorSettings.GetUsages(connectionId), .. projects.GetUsages(connectionId, keyNames)];
    }
}
