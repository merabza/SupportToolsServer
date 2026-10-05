using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ReactAppTemplates;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ProjectTemplates.GetProjectTemplateByName;

public sealed class GetProjectTemplateByNameQueryHandler :
    IQueryHandler<GetProjectTemplateByNameQuery, StsProjectTemplateDataModel>
{
    private readonly IProjectTemplateRepository _projectTemplateRepository;
    private readonly IReactAppTemplateRepository _reactAppTemplateRepository;

    public GetProjectTemplateByNameQueryHandler(IProjectTemplateRepository projectTemplateRepository,
        IReactAppTemplateRepository reactAppTemplateRepository)
    {
        _projectTemplateRepository = projectTemplateRepository;
        _reactAppTemplateRepository = reactAppTemplateRepository;
    }

    public async Task<Result<StsProjectTemplateDataModel>> Handle(GetProjectTemplateByNameQuery query,
        CancellationToken cancellationToken)
    {
        ProjectTemplate? projectTemplate = await _projectTemplateRepository.GetByName(query.Name, cancellationToken);
        if (projectTemplate is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ProjectTemplateContractMapper.EntityName,
                query.Name);
        }

        return projectTemplate.ToContractModel(
            (await _reactAppTemplateRepository.GetAll(cancellationToken)).ToNamesById());
    }
}
