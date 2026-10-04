using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DotnetTools;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DotnetTools.GetDotnetTools;

public sealed class GetDotnetToolsQueryHandler : IQueryHandler<GetDotnetToolsQuery, List<StsDotnetToolDataModel>>
{
    private readonly IDotnetToolRepository _dotnetToolRepository;

    public GetDotnetToolsQueryHandler(IDotnetToolRepository dotnetToolRepository)
    {
        _dotnetToolRepository = dotnetToolRepository;
    }

    public async Task<Result<List<StsDotnetToolDataModel>>> Handle(GetDotnetToolsQuery query,
        CancellationToken cancellationToken)
    {
        List<DotnetTool> dotnetTools = await _dotnetToolRepository.GetAll(cancellationToken);

        List<StsDotnetToolDataModel> dotnetToolModels =
        [
            .. dotnetTools.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => x.ToContractModel())
        ];
        return dotnetToolModels;
    }
}
