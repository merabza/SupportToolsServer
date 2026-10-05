using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ReactAppTemplates;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ProjectTemplates.UpdateProjectTemplate;

public sealed class UpdateProjectTemplateCommandHandler : ICommandHandler<UpdateProjectTemplateCommand, int>
{
    private readonly IProjectTemplateRepository _projectTemplateRepository;
    private readonly IReactAppTemplateRepository _reactAppTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectTemplateCommandHandler(IProjectTemplateRepository projectTemplateRepository,
        IReactAppTemplateRepository reactAppTemplateRepository, IUnitOfWork unitOfWork)
    {
        _projectTemplateRepository = projectTemplateRepository;
        _reactAppTemplateRepository = reactAppTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(UpdateProjectTemplateCommand command, CancellationToken cancellationToken)
    {
        StsProjectTemplateDataModel model = command.ProjectTemplate;

        //Version 0 — შექმნა, N — განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია
        ProjectTemplate? stored = await _projectTemplateRepository.GetByName(model.Name, cancellationToken);
        Result versionResult = RecordVersions.Check(ProjectTemplateContractMapper.EntityName, model.Name,
            model.Version, stored?.Version);
        if (versionResult.IsFailure)
        {
            return versionResult.Error;
        }

        //React-ის შაბლონი სახელით მოდის, ბაზაში კი მისი Id ინახება. ცარიელი სახელი ნიშნავს, რომ შაბლონი არ არის
        var references = new ReferencedRecords();
        ReactAppTemplate? reactTemplate = await references.Find(model.ReactTemplateName,
            ReactAppTemplateContractMapper.EntityName, _reactAppTemplateRepository.GetByName, cancellationToken);
        if (!references.AreAllFound)
        {
            return references.MissingError();
        }

        ProjectTemplate projectTemplate;
        if (stored is null)
        {
            projectTemplate = ProjectTemplate.Create(model.Name, model.SupportProjectType, model.TestProjectName,
                model.TestProjectShortName, model.UseDatabase, model.UseDbPartFolderForDatabaseProjects,
                model.UseMenu, model.UseHttps, model.UseReact, model.UseCarcass, model.UseIdentity,
                model.UseReCounter, model.UseSignalR, model.UseFluentValidation, reactTemplate?.Id);
            _projectTemplateRepository.Add(projectTemplate);
        }
        else
        {
            stored.Update(model.Name, model.SupportProjectType, model.TestProjectName, model.TestProjectShortName,
                model.UseDatabase, model.UseDbPartFolderForDatabaseProjects, model.UseMenu, model.UseHttps,
                model.UseReact, model.UseCarcass, model.UseIdentity, model.UseReCounter, model.UseSignalR,
                model.UseFluentValidation, reactTemplate?.Id);
            _projectTemplateRepository.Update(stored);
            projectTemplate = stored;
        }

        Result saveResult = await RecordVersions.SaveChanges(_unitOfWork, ProjectTemplateContractMapper.EntityName,
            model.Name, model.Version,
            async ct => (await _projectTemplateRepository.GetByName(model.Name, ct))?.Version, cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        return projectTemplate.Version;
    }
}
