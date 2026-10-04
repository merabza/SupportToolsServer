using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.Runtimes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Runtimes.DeleteRuntime;

public sealed class DeleteRuntimeCommandHandler : ICommandHandler<DeleteRuntimeCommand>
{
    private readonly IRuntimeRepository _runtimeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRuntimeCommandHandler(IRuntimeRepository runtimeRepository, IUnitOfWork unitOfWork)
    {
        _runtimeRepository = runtimeRepository;
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

        //აქ მოწმდება, ხომ არ მიმართავს ჩანაწერს სხვა აგრეგატი (409 RecordIsInUse მომხმარებლების სიით). Runtime-ს ჯერ
        //არავინ მიმართავს: B4 (Server) FK-ს Restrict-ით დაამატებს და სერვერების შემოწმებას აქ ჩასვამს

        _runtimeRepository.Delete(runtime);
        return await RecordVersions.SaveChanges(_unitOfWork, RuntimeContractMapper.EntityName, command.Name,
            runtime.Version, async ct => (await _runtimeRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
