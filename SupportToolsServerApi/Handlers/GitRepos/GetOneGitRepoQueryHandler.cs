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
public sealed class GetOneGitRepoQueryHandler : IQueryHandler<GetOneGitRepoRequestQuery, StsGitDataModel>
{
    private readonly GitsListService _gitsListService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetOneGitRepoQueryHandler(GitsListService gitsListService)
    {
        _gitsListService = gitsListService;
    }

    public async Task<OneOf<StsGitDataModel, Err[]>> Handle(GetOneGitRepoRequestQuery request,
        CancellationToken cancellationToken = default)
    {
        // Assumes request.RecordKey exists; adjust as needed
        return (await _gitsListService.GetOneGit(request.GitKey, cancellationToken))
            .Match<OneOf<StsGitDataModel, Err[]>>(f0 => f0.ToContractModel(), f1 => f1);
    }
}