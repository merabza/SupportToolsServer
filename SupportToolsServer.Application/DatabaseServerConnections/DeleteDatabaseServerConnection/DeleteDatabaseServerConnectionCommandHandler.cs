using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;

public sealed class DeleteDatabaseServerConnectionCommandHandler : ICommandHandler<DeleteDatabaseServerConnectionCommand>
{
    private readonly IDatabaseServerConnectionRepository _databaseServerConnectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDatabaseServerConnectionCommandHandler(
        IDatabaseServerConnectionRepository databaseServerConnectionRepository, IUnitOfWork unitOfWork)
    {
        _databaseServerConnectionRepository = databaseServerConnectionRepository;
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

        //აქ მოწმდება, ხომ არ მიმართავს ჩანაწერს სხვა აგრეგატი (409 RecordIsInUse მომხმარებლების სიით).
        //DatabaseServerConnection-ს ჯერ არავინ მიმართავს: B5 (ProjectCreatorSettings) და B6/B7 (ბაზის პარამეტრები) FK-ს
        //Restrict-ით დაამატებენ და მომხმარებლების შემოწმებას აქ ჩასვამენ. folders set-ები კავშირთან ერთად იშლება

        _databaseServerConnectionRepository.Delete(connection);
        return await RecordVersions.SaveChanges(_unitOfWork, DatabaseServerConnectionContractMapper.EntityName,
            command.Name, connection.Version,
            async ct => (await _databaseServerConnectionRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
