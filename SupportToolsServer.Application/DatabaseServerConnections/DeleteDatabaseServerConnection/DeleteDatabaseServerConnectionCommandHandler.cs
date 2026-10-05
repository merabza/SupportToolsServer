using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Settings;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.Settings;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;

public sealed class DeleteDatabaseServerConnectionCommandHandler : ICommandHandler<DeleteDatabaseServerConnectionCommand>
{
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;
    private readonly IProjectCreatorSettingsRepository _projectCreatorSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDatabaseServerConnectionCommandHandler(
        IDatabaseServerConnectionRepository databaseServerConnectionRepository,
        IProjectCreatorSettingsRepository projectCreatorSettingsRepository, IUnitOfWork unitOfWork)
    {
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
        _projectCreatorSettingsRepository = projectCreatorSettingsRepository;
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

        //DatabaseServerConnection-ს სხვა აგრეგატი მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409
        //RecordIsInUse მომხმარებლების სიით. B6/B7 (ბაზის პარამეტრები) თავის მომხმარებლებს აქ დაამატებენ. folders
        //set-ები კავშირთან ერთად იშლება
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

    //მომხმარებლები: პროექტის შემქმნელის პარამეტრების ველი ("ProjectCreatorSettings.<ველი>")
    private async Task<List<string>> GetUsages(DatabaseServerConnectionId connectionId,
        CancellationToken cancellationToken)
    {
        ProjectCreatorSettings? projectCreatorSettings =
            await _projectCreatorSettingsRepository.Get(cancellationToken);

        return [.. projectCreatorSettings.GetUsages(connectionId)];
    }
}
