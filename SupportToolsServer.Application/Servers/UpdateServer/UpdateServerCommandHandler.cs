using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Runtimes;
using SupportToolsServerApiContracts.Errors;
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
        List<(string EntityName, string Name)> missing = [];
        ApiClient? webAgent = await FindReferenced(model.WebAgentName, ApiClientContractMapper.EntityName,
            _apiClientRepository.GetByName, missing, cancellationToken);
        ApiClient? webAgentInstaller = await FindReferenced(model.WebAgentInstallerName,
            ApiClientContractMapper.EntityName, _apiClientRepository.GetByName, missing, cancellationToken);
        Runtime? runtime = await FindReferenced(model.Runtime, RuntimeContractMapper.EntityName,
            _runtimeRepository.GetByName, missing, cancellationToken);
        if (missing.Count > 0)
        {
            //ორივე ვებაგენტი ერთი და იგივე ApiClient შეიძლება იყოს, ამიტომ სახელი ერთხელ იწერება
            return SupportToolsServerApiClientErrors.ReferencedRecordsNotFound(missing.Distinct()
                .ToLookup(x => x.EntityName, x => x.Name));
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

    //სახელით მითითებული ჩანაწერი. ცარიელი სახელი მითითება არ არის, არარსებული სახელი კი missing-ს ემატება
    private static async Task<TEntity?> FindReferenced<TEntity>(string? name, string entityName,
        Func<string, CancellationToken, Task<TEntity?>> getByName, List<(string EntityName, string Name)> missing,
        CancellationToken cancellationToken) where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        TEntity? entity = await getByName(name, cancellationToken);
        if (entity is null)
        {
            missing.Add((entityName, name));
        }

        return entity;
    }
}
