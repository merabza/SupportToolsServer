using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using SupportToolsServerApi.QueryRequests;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApplication.Services.Gits.List;
using SupportToolsServerMappers;
using SystemTools.MediatRMessagingAbstractions;
using SystemTools.SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitRepos;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class GetGitReposQueryHandler : IQueryHandler<GetGitReposRequestQuery, List<StsGitDataModel>>
{
    private readonly GitsListService _gitsListService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetGitReposQueryHandler(GitsListService gitsListService)
    {
        _gitsListService = gitsListService;
    }

    public async Task<OneOf<List<StsGitDataModel>, Error[]>> Handle(GetGitReposRequestQuery request,
        CancellationToken cancellationToken)
    {
        return (await _gitsListService.GetGits(cancellationToken)).Match<OneOf<List<StsGitDataModel>, Error[]>>(
            f0 => f0.Select(x => x.ToContractModel()).ToList(), f1 => f1);
    }
}
