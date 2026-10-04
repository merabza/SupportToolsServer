using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.DotnetTools;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DotnetTools.DeleteDotnetTool;

public sealed class DeleteDotnetToolCommandHandler : ICommandHandler<DeleteDotnetToolCommand>
{
    private readonly IDotnetToolRepository _dotnetToolRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDotnetToolCommandHandler(IDotnetToolRepository dotnetToolRepository, IUnitOfWork unitOfWork)
    {
        _dotnetToolRepository = dotnetToolRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteDotnetToolCommand command, CancellationToken cancellationToken)
    {
        DotnetTool? dotnetTool = await _dotnetToolRepository.GetByName(command.Name, cancellationToken);
        if (dotnetTool is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(DotnetToolContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != dotnetTool.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(DotnetToolContractMapper.EntityName,
                command.Name, command.Version.Value, dotnetTool.Version);
        }

        //DotnetTool-ს სხვა აგრეგატი არ მიმართავს (README §4.2), ამიტომ მომხმარებლების შემოწმება (409 RecordIsInUse) აქ
        //არ არის. თუ მიმართვა გაჩნდება, შემოწმება აქ ჩაჯდება, როგორც სხვა ცნობარებში

        _dotnetToolRepository.Delete(dotnetTool);
        return await RecordVersions.SaveChanges(_unitOfWork, DotnetToolContractMapper.EntityName, command.Name,
            dotnetTool.Version, async ct => (await _dotnetToolRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
