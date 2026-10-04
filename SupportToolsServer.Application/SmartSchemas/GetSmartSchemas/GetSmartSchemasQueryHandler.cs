using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.SmartSchemas.GetSmartSchemas;

public sealed class GetSmartSchemasQueryHandler : IQueryHandler<GetSmartSchemasQuery, List<StsSmartSchemaDataModel>>
{
    private readonly ISmartSchemaRepository _smartSchemaRepository;

    public GetSmartSchemasQueryHandler(ISmartSchemaRepository smartSchemaRepository)
    {
        _smartSchemaRepository = smartSchemaRepository;
    }

    public async Task<Result<List<StsSmartSchemaDataModel>>> Handle(GetSmartSchemasQuery query,
        CancellationToken cancellationToken)
    {
        List<SmartSchema> smartSchemas = await _smartSchemaRepository.GetAll(cancellationToken);

        List<StsSmartSchemaDataModel> smartSchemaModels =
        [
            .. smartSchemas.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel())
        ];
        return smartSchemaModels;
    }
}
