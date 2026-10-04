using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DotnetTools;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DotnetTools.UpdateDotnetTool;

public sealed class UpdateDotnetToolCommandHandler : ICommandHandler<UpdateDotnetToolCommand, int>
{
    private readonly IDotnetToolRepository _dotnetToolRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDotnetToolCommandHandler(IDotnetToolRepository dotnetToolRepository, IUnitOfWork unitOfWork)
    {
        _dotnetToolRepository = dotnetToolRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateDotnetToolCommand command, CancellationToken cancellationToken)
    {
        StsDotnetToolDataModel model = command.DotnetTool;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        DotnetTool? stored = await _dotnetToolRepository.GetByName(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(DotnetToolContractMapper.EntityName, model.Name, model.Version,
            stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        DotnetTool dotnetTool;
        if (stored is null)
        {
            dotnetTool = DotnetTool.Create(model.Name, model.PackageId, model.MaxVersion, model.Description);
            _dotnetToolRepository.Add(dotnetTool);
        }
        else
        {
            stored.Update(model.Name, model.PackageId, model.MaxVersion, model.Description);
            _dotnetToolRepository.Update(stored);
            dotnetTool = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, DotnetToolContractMapper.EntityName,
            model.Name, model.Version, async ct => (await _dotnetToolRepository.GetByName(model.Name, ct))?.Version,
            cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return dotnetTool.Version;
    }
}
