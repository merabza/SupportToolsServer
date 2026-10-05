using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.ReactAppTemplates;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ProjectTemplates.GetProjectTemplates;

public sealed class GetProjectTemplatesQueryHandler :
    IQueryHandler<GetProjectTemplatesQuery, List<StsProjectTemplateDataModel>>
{
    private readonly IProjectTemplateRepository _projectTemplateRepository;
    private readonly IReactAppTemplateRepository _reactAppTemplateRepository;

    public GetProjectTemplatesQueryHandler(IProjectTemplateRepository projectTemplateRepository,
        IReactAppTemplateRepository reactAppTemplateRepository)
    {
        _projectTemplateRepository = projectTemplateRepository;
        _reactAppTemplateRepository = reactAppTemplateRepository;
    }

    public async Task<Result<List<StsProjectTemplateDataModel>>> Handle(GetProjectTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        List<ProjectTemplate> projectTemplates = await _projectTemplateRepository.GetAll(cancellationToken);
        Dictionary<ReactAppTemplateId, string> reactAppTemplateNames =
            (await _reactAppTemplateRepository.GetAll(cancellationToken)).ToNamesById();

        List<StsProjectTemplateDataModel> projectTemplateModels =
        [
            .. projectTemplates.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.ToContractModel(reactAppTemplateNames))
        ];
        return projectTemplateModels;
    }
}
