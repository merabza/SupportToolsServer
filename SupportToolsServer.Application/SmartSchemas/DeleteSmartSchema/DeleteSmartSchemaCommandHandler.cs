using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;

public sealed class DeleteSmartSchemaCommandHandler : ICommandHandler<DeleteSmartSchemaCommand>
{
    private readonly ISmartSchemaRepository _smartSchemaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSmartSchemaCommandHandler(ISmartSchemaRepository smartSchemaRepository, IUnitOfWork unitOfWork)
    {
        _smartSchemaRepository = smartSchemaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteSmartSchemaCommand command, CancellationToken cancellationToken)
    {
        SmartSchema? smartSchema = await _smartSchemaRepository.GetByName(command.Name, cancellationToken);
        if (smartSchema is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(SmartSchemaContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != smartSchema.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(SmartSchemaContractMapper.EntityName,
                command.Name, command.Version.Value, smartSchema.Version);
        }

        //აქ მოწმდება, ხომ არ მიმართავს ჩანაწერს სხვა აგრეგატი (409 RecordIsInUse მომხმარებლების სიით). SmartSchema-ს ჯერ
        //არავინ მიმართავს: B5 (GlobalSettings, ProjectCreatorSettings) და B6/B7 (ბაზის პარამეტრები) FK-ს Restrict-ით
        //დაამატებენ და მომხმარებლების შემოწმებას აქ ჩასვამენ. დეტალები სქემასთან ერთად იშლება

        _smartSchemaRepository.Delete(smartSchema);
        return await RecordVersions.SaveChanges(_unitOfWork, SmartSchemaContractMapper.EntityName, command.Name,
            smartSchema.Version, async ct => (await _smartSchemaRepository.GetByName(command.Name, ct))?.Version,
            cancellationToken);
    }
}
