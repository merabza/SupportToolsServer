using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Runtimes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.Runtimes.UpdateRuntime;

public sealed class UpdateRuntimeCommandHandler : ICommandHandler<UpdateRuntimeCommand, int>
{
    private readonly IRuntimeRepository _runtimeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRuntimeCommandHandler(IRuntimeRepository runtimeRepository, IUnitOfWork unitOfWork)
    {
        _runtimeRepository = runtimeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateRuntimeCommand command, CancellationToken cancellationToken)
    {
        StsRuntimeDataModel model = command.Runtime;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        Runtime? stored = await _runtimeRepository.GetByName(model.Name, cancellationToken);
        Result versionResult =
            RecordVersions.Check(RuntimeContractMapper.EntityName, model.Name, model.Version, stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        Runtime runtime;
        if (stored is null)
        {
            runtime = Runtime.Create(model.Name, model.Description);
            _runtimeRepository.Add(runtime);
        }
        else
        {
            stored.Update(model.Name, model.Description);
            _runtimeRepository.Update(stored);
            runtime = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, RuntimeContractMapper.EntityName, model.Name,
            model.Version, async ct => (await _runtimeRepository.GetByName(model.Name, ct))?.Version,
            cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return runtime.Version;
    }
}
