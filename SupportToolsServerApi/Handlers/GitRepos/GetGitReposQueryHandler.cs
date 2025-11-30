using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.QueryRequests;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApplication.Services.Gits.List;
using SupportToolsServerMappers;
using SystemToolsShared.Errors;

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

    public async Task<OneOf<List<StsGitDataModel>, Err[]>> Handle(GetGitReposRequestQuery request,
        CancellationToken cancellationToken = default)
    {
        return (await _gitsListService.GetGits(cancellationToken)).Match<OneOf<List<StsGitDataModel>, Err[]>>(
            f0 => f0.Select(x => x.ToContractModel()).ToList(), f1 => f1);
    }
}