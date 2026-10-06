using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.DatabaseServerConnections;
using SupportToolsServer.Application.Environments;
using SupportToolsServer.Application.Projects;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Servers;
using SupportToolsServer.Application.Settings;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ApiClients.DeleteApiClient;

public sealed class DeleteApiClientCommandHandler : ICommandHandler<DeleteApiClientCommand>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;
    private readonly IDeploymentEnvironmentRepository _environmentRepository;
    private readonly IGlobalSettingsRepository _globalSettingsRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IServerRepository _serverRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteApiClientCommandHandler(IApiClientRepository apiClientRepository,
        IDatabaseServerConnectionRepository databaseServerConnectionRepository, IServerRepository serverRepository,
        IGlobalSettingsRepository globalSettingsRepository, IProjectRepository projectRepository,
        IDeploymentEnvironmentRepository environmentRepository, IUnitOfWork unitOfWork)
    {
        _apiClientRepository = apiClientRepository;
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _serverRepository = serverRepository;
        _globalSettingsRepository = globalSettingsRepository;
        _projectRepository = projectRepository;
        _environmentRepository = environmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteApiClientCommand command, CancellationToken cancellationToken)
    {
        ApiClient? apiClient = await _apiClientRepository.GetByName(command.Name, cancellationToken);
        if (apiClient is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ApiClientContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != apiClient.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(ApiClientContractMapper.EntityName,
                command.Name, command.Version.Value, apiClient.Version);
        }

        //ApiClient-ს სხვა აგრეგატები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse
        //მომხმარებლების სიით
        List<string> usages = await GetUsages(apiClient.Id, cancellationToken);
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(ApiClientContractMapper.EntityName, command.Name,
                usages);
        }

        _apiClientRepository.Delete(apiClient);
        return await RecordVersions.SaveChanges(_unitOfWork, ApiClientContractMapper.EntityName, command.Name,
            apiClient.Version, async ct => (await _apiClientRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }

    //მომხმარებლები "<ტიპი> <სახელი>" ფორმით: ჯერ ბაზის კავშირები, მერე სერვერები, თითოეული სახელით დალაგებული.
    //სერვერი ერთხელ ჩანს, თუნდაც ApiClient მისი ორივე ვებაგენტი იყოს. მერე გლობალური პარამეტრების ველი
    //("GlobalSettings.<ველი>") და ბოლოს პროექტების ServerInfo-ები, რომლებიც მას ვებაგენტად იყენებს ვერსიის
    //შესამოწმებლად ("Project <სახელი> / <სერვერი>|<გარემო>")
    private async Task<List<string>> GetUsages(ApiClientId apiClientId, CancellationToken cancellationToken)
    {
        List<DatabaseServerConnection> connections = await _databaseServerConnectionRepository.GetAll(cancellationToken);
        List<Server> servers = await _serverRepository.GetAll(cancellationToken);
        GlobalSettings? globalSettings = await _globalSettingsRepository.Get(cancellationToken);
        List<Project> projects = await _projectRepository.GetAll(cancellationToken);
        var keyNames = new ServerInfoKeyNames
        {
            Servers = servers.ToNamesById(),
            Environments = (await _environmentRepository.GetAll(cancellationToken)).ToNamesById()
        };

        return
        [
            .. connections.Where(x => apiClientId.Equals(x.DbWebAgentId)).Select(x => x.Name)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(x => $"{DatabaseServerConnectionContractMapper.EntityName} {x}"),
            .. servers.Where(x => apiClientId.Equals(x.WebAgentId) || apiClientId.Equals(x.WebAgentInstallerId))
                .Select(x => x.Name).Order(StringComparer.OrdinalIgnoreCase)
                .Select(x => $"{ServerContractMapper.EntityName} {x}"),
            .. globalSettings.GetUsages(apiClientId), .. projects.GetUsages(apiClientId, keyNames)
        ];
    }
}
