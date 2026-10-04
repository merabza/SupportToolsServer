using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ReactAppTemplates.UpdateReactAppTemplate;

public sealed class UpdateReactAppTemplateCommandHandler : ICommandHandler<UpdateReactAppTemplateCommand, int>
{
    private readonly IReactAppTemplateRepository _reactAppTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateReactAppTemplateCommandHandler(IReactAppTemplateRepository reactAppTemplateRepository,
        IUnitOfWork unitOfWork)
    {
        _reactAppTemplateRepository = reactAppTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateReactAppTemplateCommand command, CancellationToken cancellationToken)
    {
        StsReactAppTemplateDataModel model = command.ReactAppTemplate;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        ReactAppTemplate? stored = await _reactAppTemplateRepository.GetByName(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(ReactAppTemplateContractMapper.EntityName, model.Name,
            model.Version, stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        ReactAppTemplate reactAppTemplate;
        if (stored is null)
        {
            reactAppTemplate = ReactAppTemplate.Create(model.Name, model.Template);
            _reactAppTemplateRepository.Add(reactAppTemplate);
        }
        else
        {
            stored.Update(model.Name, model.Template);
            _reactAppTemplateRepository.Update(stored);
            reactAppTemplate = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, ReactAppTemplateContractMapper.EntityName,
            model.Name, model.Version,
            async ct => (await _reactAppTemplateRepository.GetByName(model.Name, ct))?.Version, cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return reactAppTemplate.Version;
    }
}
