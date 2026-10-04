using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplates;

public sealed class GetReactAppTemplatesQueryHandler :
    IQueryHandler<GetReactAppTemplatesQuery, List<StsReactAppTemplateDataModel>>
{
    private readonly IReactAppTemplateRepository _reactAppTemplateRepository;

    public GetReactAppTemplatesQueryHandler(IReactAppTemplateRepository reactAppTemplateRepository)
    {
        _reactAppTemplateRepository = reactAppTemplateRepository;
    }

    public async Task<Result<List<StsReactAppTemplateDataModel>>> Handle(GetReactAppTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        List<ReactAppTemplate> reactAppTemplates = await _reactAppTemplateRepository.GetAll(cancellationToken);

        List<StsReactAppTemplateDataModel> reactAppTemplateModels =
        [
            .. reactAppTemplates.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel())
        ];
        return reactAppTemplateModels;
    }
}
