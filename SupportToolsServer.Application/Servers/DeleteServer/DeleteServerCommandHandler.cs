using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.Servers;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Servers.DeleteServer;

public sealed class DeleteServerCommandHandler : ICommandHandler<DeleteServerCommand>
{
    private readonly IServerRepository _serverRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServerCommandHandler(IServerRepository serverRepository, IUnitOfWork unitOfWork)
    {
        _serverRepository = serverRepository;
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

        //აქ მოწმდება, ხომ არ მიმართავს ჩანაწერს სხვა აგრეგატი (409 RecordIsInUse მომხმარებლების სიით). Server-ს ჯერ
        //არავინ მიმართავს: B5 (ProjectCreatorSettings.ProductionServerName) და B7 (ServerInfo) FK-ს Restrict-ით
        //დაამატებენ და მომხმარებლების შემოწმებას აქ ჩასვამენ

        _serverRepository.Delete(server);
        return await RecordVersions.SaveChanges(_unitOfWork, ServerContractMapper.EntityName, command.Name,
            server.Version, async ct => (await _serverRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
