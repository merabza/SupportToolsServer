using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.SmartSchemas.UpdateSmartSchema;

public sealed class UpdateSmartSchemaCommandHandler : ICommandHandler<UpdateSmartSchemaCommand, int>
{
    private readonly ISmartSchemaRepository _smartSchemaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSmartSchemaCommandHandler(ISmartSchemaRepository smartSchemaRepository, IUnitOfWork unitOfWork)
    {
        _smartSchemaRepository = smartSchemaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateSmartSchemaCommand command, CancellationToken cancellationToken)
    {
        StsSmartSchemaDataModel model = command.SmartSchema;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია. სქემა თვალყურის დევნებით იკითხება,
        //რომ შენახვისას ჩანაცვლებული დეტალები წაიშალოს
        SmartSchema? stored = await _smartSchemaRepository.GetByNameForUpdate(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(SmartSchemaContractMapper.EntityName, model.Name, model.Version,
            stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        List<SmartSchemaDetail> details =
            [.. model.Details.Select(x => SmartSchemaDetail.Create(x.PeriodType, x.PreserveCount))];
        SmartSchema smartSchema;
        if (stored is null)
        {
            smartSchema = SmartSchema.Create(model.Name, model.LastPreserveCount, details);
            _smartSchemaRepository.Add(smartSchema);
        }
        else
        {
            stored.Update(model.Name, model.LastPreserveCount, details);
            _smartSchemaRepository.Update(stored);
            smartSchema = stored;
        }

        //შენახვის ჩავარდნისას ვერსია თავიდან თვალყურის დევნების გარეშე იკითხება, რომ ბაზის მნიშვნელობა მივიღოთ და არა
        //ამ მოთხოვნის მიერ შეცვლილი სქემისა
        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, SmartSchemaContractMapper.EntityName,
            model.Name, model.Version, async ct => (await _smartSchemaRepository.GetByName(model.Name, ct))?.Version,
            cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return smartSchema.Version;
    }
}
