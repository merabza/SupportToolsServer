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
public sealed class GetOneGitRepoQueryHandler : IQueryHandler<GetOneGitRepoRequestQuery, StsGitDataModel>
{
    private readonly GitsListService _gitsListService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetOneGitRepoQueryHandler(GitsListService gitsListService)
    {
        _gitsListService = gitsListService;
    }

    public async Task<OneOf<StsGitDataModel, Error[]>> Handle(GetOneGitRepoRequestQuery request,
        CancellationToken cancellationToken)
    {
        // Assumes request.RecordKey exists; adjust as needed
        return (await _gitsListService.GetOneGit(request.GitKey, cancellationToken))
            .Match<OneOf<StsGitDataModel, Error[]>>(f0 => f0.ToContractModel(), f1 => f1);
    }
}
