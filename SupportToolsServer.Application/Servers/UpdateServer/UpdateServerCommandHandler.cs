using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Runtimes;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Servers.UpdateServer;

public sealed class UpdateServerCommandHandler : ICommandHandler<UpdateServerCommand, int>
{
    private readonly IApiClientRepository _apiClientRepository;
    private readonly IRuntimeRepository _runtimeRepository;
    private readonly IServerRepository _serverRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateServerCommandHandler(IServerRepository serverRepository, IApiClientRepository apiClientRepository,
        IRuntimeRepository runtimeRepository, IUnitOfWork unitOfWork)
    {
        _serverRepository = serverRepository;
        _apiClientRepository = apiClientRepository;
        _runtimeRepository = runtimeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateServerCommand command, CancellationToken cancellationToken)
    {
        StsServerDataModel model = command.Server;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        Server? stored = await _serverRepository.GetByName(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(ServerContractMapper.EntityName, model.Name, model.Version,
            stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        //ვებაგენტები და Runtime სახელით მოდის, ბაზაში კი მათი Id-ები ინახება. ცარიელი სახელი ნიშნავს, რომ მითითება არ
        //არის. ყველა არარსებული სახელი ერთ შეცდომაში ბრუნდება, ჩანაწერის ტიპებად დაჯგუფებული
        var references = new ReferencedRecords();
        ApiClient? webAgent = await references.Find(model.WebAgentName, ApiClientContractMapper.EntityName,
            _apiClientRepository.GetByName, cancellationToken);
        ApiClient? webAgentInstaller = await references.Find(model.WebAgentInstallerName,
            ApiClientContractMapper.EntityName, _apiClientRepository.GetByName, cancellationToken);
        Runtime? runtime = await references.Find(model.Runtime, RuntimeContractMapper.EntityName,
            _runtimeRepository.GetByName, cancellationToken);
        if (!references.AreAllFound)
        {
            return references.MissingError();
        }

        Server server;
        if (stored is null)
        {
            server = Server.Create(model.Name, webAgent?.Id, webAgentInstaller?.Id, model.FilesUserName,
                model.FilesUsersGroupName, runtime?.Id, model.ServerSideDownloadFolder, model.ServerSideDeployFolder);
            _serverRepository.Add(server);
        }
        else
        {
            stored.Update(model.Name, webAgent?.Id, webAgentInstaller?.Id, model.FilesUserName,
                model.FilesUsersGroupName, runtime?.Id, model.ServerSideDownloadFolder, model.ServerSideDeployFolder);
            _serverRepository.Update(stored);
            server = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, ServerContractMapper.EntityName, model.Name,
            model.Version, async ct => (await _serverRepository.GetByName(model.Name, ct))?.Version,
            cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return server.Version;
    }
}
