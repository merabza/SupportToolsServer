using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.QueryRequests;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerDom;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitRepos;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class GetGitReposQueryHandler : IQueryHandler<GetGitReposQueryRequest, List<GitDataDto>>
{
    private readonly IGitsQueriesRepository _gitsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetGitReposQueryHandler(IGitsQueriesRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public async Task<OneOf<List<GitDataDto>, Err[]>> Handle(GetGitReposQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        //ჩაიტვირთოს დერივაციის ფორმულები, ყველა
        return await _gitsRepo.GetGitRepos(cancellationToken);
    }
}