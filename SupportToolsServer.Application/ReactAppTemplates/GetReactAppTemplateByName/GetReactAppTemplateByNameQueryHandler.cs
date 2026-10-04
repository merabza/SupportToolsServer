using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplateByName;

public sealed class GetReactAppTemplateByNameQueryHandler :
    IQueryHandler<GetReactAppTemplateByNameQuery, StsReactAppTemplateDataModel>
{
    private readonly IReactAppTemplateRepository _reactAppTemplateRepository;

    public GetReactAppTemplateByNameQueryHandler(IReactAppTemplateRepository reactAppTemplateRepository)
    {
        _reactAppTemplateRepository = reactAppTemplateRepository;
    }

    public async Task<Result<StsReactAppTemplateDataModel>> Handle(GetReactAppTemplateByNameQuery query,
        CancellationToken cancellationToken)
    {
        ReactAppTemplate? reactAppTemplate = await _reactAppTemplateRepository.GetByName(query.Name, cancellationToken);
        if (reactAppTemplate is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ReactAppTemplateContractMapper.EntityName,
                query.Name);
        }

        return reactAppTemplate.ToContractModel();
    }
}
