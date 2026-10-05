using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Settings;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Servers.DeleteServer;

public sealed class DeleteServerCommandHandler : ICommandHandler<DeleteServerCommand>
{
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly IServerRepository _serverRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServerCommandHandler(IServerRepository serverRepository,
        IProjectCreatorSettingsRepository projectCreatorSettingsRepository, IUnitOfWork unitOfWork)
    {
        _serverRepository = serverRepository;
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
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

        //Server-ს სხვა აგრეგატი მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse
        //მომხმარებლების სიით. B7 (ServerInfo) თავის მომხმარებლებს აქ დაამატებს
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

    //მომხმარებლები: პროექტის შემქმნელის პარამეტრების ველი ("ProjectCreatorSettings.<ველი>")
    private async Task<List<string>> GetUsages(ServerId serverId, CancellationToken cancellationToken)
    {
        ProjectCreatorSettings? projectCreatorSettings =
            await _projectCreatorSettingsRepository.Get(cancellationToken);

        return [.. projectCreatorSettings.GetUsages(serverId)];
    }
}
