using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ProjectTemplates.DeleteProjectTemplate;

public sealed class DeleteProjectTemplateCommandHandler : ICommandHandler<DeleteProjectTemplateCommand>
{
    private readonly IProjectTemplateRepository _projectTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProjectTemplateCommandHandler(IProjectTemplateRepository projectTemplateRepository,
        IUnitOfWork unitOfWork)
    {
        _projectTemplateRepository = projectTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProjectTemplateCommand command, CancellationToken cancellationToken)
    {
        ProjectTemplate? projectTemplate = await _projectTemplateRepository.GetByName(command.Name, cancellationToken);
        if (projectTemplate is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ProjectTemplateContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != projectTemplate.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(ProjectTemplateContractMapper.EntityName,
                command.Name, command.Version.Value, projectTemplate.Version);
        }

        //ProjectTemplate-ს სხვა აგრეგატი არ მიმართავს (კლიენტის პროექტი შაბლონის სახელს არ ინახავს), ამიტომ
        //მომხმარებლების შემოწმება (409 RecordIsInUse) აქ არ არის. თუ მიმართვა გაჩნდება, შემოწმება აქ ჩაჯდება

        _projectTemplateRepository.Delete(projectTemplate);
        return await RecordVersions.SaveChanges(_unitOfWork, ProjectTemplateContractMapper.EntityName, command.Name,
            projectTemplate.Version,
            async ct => (await _projectTemplateRepository.GetByName(command.Name, ct))?.Version, cancellationToken);
    }
}
