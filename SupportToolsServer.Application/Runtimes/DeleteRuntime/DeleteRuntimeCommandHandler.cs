using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServer.Application.Servers;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Runtimes.DeleteRuntime;

public sealed class DeleteRuntimeCommandHandler : ICommandHandler<DeleteRuntimeCommand>
{
    private readonly IRuntimeRepository _runtimeRepository;
    private readonly IServerRepository _serverRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRuntimeCommandHandler(IRuntimeRepository runtimeRepository, IServerRepository serverRepository,
        IUnitOfWork unitOfWork)
    {
        _runtimeRepository = runtimeRepository;
        _serverRepository = serverRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteRuntimeCommand command, CancellationToken cancellationToken)
    {
        Runtime? runtime = await _runtimeRepository.GetByName(command.Name, cancellationToken);
        if (runtime is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(RuntimeContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != runtime.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(RuntimeContractMapper.EntityName, command.Name,
                command.Version.Value, runtime.Version);
        }

        //Runtime-ს სერვერები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse მომხმარებლების
        //სიით
        List<string> usages = await GetUsages(runtime.Id, cancellationToken);
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(RuntimeContractMapper.EntityName, command.Name,
                usages);
        }

        _runtimeRepository.Delete(runtime);
        return await RecordVersions.SaveChanges(_unitOfWork, RuntimeContractMapper.EntityName, command.Name,
            runtime.Version, async ct => (await _runtimeRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }

    //მომხმარებლები "<ტიპი> <სახელი>" ფორმით, სახელით დალაგებული
    private async Task<List<string>> GetUsages(RuntimeId runtimeId, CancellationToken cancellationToken)
    {
        List<Server> servers = await _serverRepository.GetAll(cancellationToken);

        return
        [
            .. servers.Where(x => runtimeId.Equals(x.RuntimeId)).Select(x => x.Name)
                .Order(StringComparer.OrdinalIgnoreCase).Select(x => $"{ServerContractMapper.EntityName} {x}")
        ];
    }
}
