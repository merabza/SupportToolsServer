using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.QueryRequests;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerDom;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class GetGitReposQueryHandler : IQueryHandler<GetGitReposQueryRequest, List<GitDataDomain>>
{
    private readonly IGitsRepository _gitsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetGitReposQueryHandler(IGitsRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public async Task<OneOf<List<GitDataDomain>, IEnumerable<Err>>> Handle(GetGitReposQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        //ჩაიტვირთოს დერივაციის ფორმულები, ყველა
        return await _gitsRepo.GetGitRepos(cancellationToken);
    }
}