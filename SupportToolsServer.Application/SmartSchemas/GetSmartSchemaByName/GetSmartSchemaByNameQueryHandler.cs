using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.SmartSchemas.GetSmartSchemaByName;

public sealed class GetSmartSchemaByNameQueryHandler : IQueryHandler<GetSmartSchemaByNameQuery, StsSmartSchemaDataModel>
{
    private readonly ISmartSchemaRepository _smartSchemaRepository;

    public GetSmartSchemaByNameQueryHandler(ISmartSchemaRepository smartSchemaRepository)
    {
        _smartSchemaRepository = smartSchemaRepository;
    }

    public async Task<Result<StsSmartSchemaDataModel>> Handle(GetSmartSchemaByNameQuery query,
        CancellationToken cancellationToken)
    {
        SmartSchema? smartSchema = await _smartSchemaRepository.GetByName(query.Name, cancellationToken);
        if (smartSchema is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(SmartSchemaContractMapper.EntityName,
                query.Name);
        }

        return smartSchema.ToContractModel();
    }
}
