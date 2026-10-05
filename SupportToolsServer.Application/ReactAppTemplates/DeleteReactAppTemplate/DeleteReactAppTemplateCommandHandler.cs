using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ProjectTemplates;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ReactAppTemplates.DeleteReactAppTemplate;

public sealed class DeleteReactAppTemplateCommandHandler : ICommandHandler<DeleteReactAppTemplateCommand>
{
    private readonly IProjectTemplateRepository _projectTemplateRepository;
    private readonly IReactAppTemplateRepository _reactAppTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReactAppTemplateCommandHandler(IReactAppTemplateRepository reactAppTemplateRepository,
        IProjectTemplateRepository projectTemplateRepository, IUnitOfWork unitOfWork)
    {
        _reactAppTemplateRepository = reactAppTemplateRepository;
        _projectTemplateRepository = projectTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteReactAppTemplateCommand command, CancellationToken cancellationToken)
    {
        ReactAppTemplate? reactAppTemplate =
            await _reactAppTemplateRepository.GetByName(command.Name, cancellationToken);
        if (reactAppTemplate is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ReactAppTemplateContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != reactAppTemplate.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(ReactAppTemplateContractMapper.EntityName,
                command.Name, command.Version.Value, reactAppTemplate.Version);
        }

        //ReactAppTemplate-ს პროექტის შაბლონები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409
        //RecordIsInUse მომხმარებლების სიით
        List<string> usages = await GetUsages(reactAppTemplate.Id, cancellationToken);
        if (usages.Count > 0)
        {
            return SupportToolsServerApiClientErrors.RecordIsInUse(ReactAppTemplateContractMapper.EntityName,
                command.Name, usages);
        }

        _reactAppTemplateRepository.Delete(reactAppTemplate);
        return await RecordVersions.SaveChanges(_unitOfWork, ReactAppTemplateContractMapper.EntityName, command.Name,
            reactAppTemplate.Version,
            async ct => (await _reactAppTemplateRepository.GetByName(command.Name, ct))?.Version, cancellationToken);
    }

    //მომხმარებლები "<ტიპი> <სახელი>" ფორმით, სახელით დალაგებული
    private async Task<List<string>> GetUsages(ReactAppTemplateId reactAppTemplateId,
        CancellationToken cancellationToken)
    {
        List<ProjectTemplate> projectTemplates = await _projectTemplateRepository.GetAll(cancellationToken);

        return
        [
            .. projectTemplates.Where(x => reactAppTemplateId.Equals(x.ReactTemplateId)).Select(x => x.Name)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(x => $"{ProjectTemplateContractMapper.EntityName} {x}")
        ];
    }
}
