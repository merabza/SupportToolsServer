using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DotnetTools;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.DotnetTools.GetDotnetToolByName;

public sealed class GetDotnetToolByNameQueryHandler : IQueryHandler<GetDotnetToolByNameQuery, StsDotnetToolDataModel>
{
    private readonly IDotnetToolRepository _dotnetToolRepository;

    public GetDotnetToolByNameQueryHandler(IDotnetToolRepository dotnetToolRepository)
    {
        _dotnetToolRepository = dotnetToolRepository;
    }

    public async Task<Result<StsDotnetToolDataModel>> Handle(GetDotnetToolByNameQuery query,
        CancellationToken cancellationToken)
    {
        DotnetTool? dotnetTool = await _dotnetToolRepository.GetByName(query.Name, cancellationToken);
        if (dotnetTool is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(DotnetToolContractMapper.EntityName,
                query.Name);
        }

        return dotnetTool.ToContractModel();
    }
}
