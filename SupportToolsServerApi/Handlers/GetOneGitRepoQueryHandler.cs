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
public sealed class GetOneGitRepoQueryHandler : IQueryHandler<GetOneGitRepoQueryRequest, GitDataDto>
{
    private readonly IGitsRepository _gitsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetOneGitRepoQueryHandler(IGitsRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public async Task<OneOf<GitDataDto, Err[]>> Handle(GetOneGitRepoQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        // Assumes request.RecordKey exists; adjust as needed
        return await _gitsRepo.GetGitRepoByKey(request.GitKey, cancellationToken);
    }
}