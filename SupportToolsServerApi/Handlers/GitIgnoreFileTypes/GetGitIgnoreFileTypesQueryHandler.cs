using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerApi.QueryRequests;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.List;
using SupportToolsServerMappers;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitIgnoreFileTypes;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class
    GetGitIgnoreFileTypesQueryHandler : IQueryHandler<GetGitIgnoreFileTypesRequestQuery,
    List<StsGitIgnoreFileTypeDataModel>>
{
    private readonly GitIgnoreFileTypeListService _gitIgnoreFileTypeListService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetGitIgnoreFileTypesQueryHandler(GitIgnoreFileTypeListService gitIgnoreFileTypeListService)
    {
        _gitIgnoreFileTypeListService = gitIgnoreFileTypeListService;
    }

    public async Task<OneOf<List<StsGitIgnoreFileTypeDataModel>, Err[]>> Handle(
        GetGitIgnoreFileTypesRequestQuery request, CancellationToken cancellationToken = default)
    {
        var getGitIgnoreFileTypesResult = await _gitIgnoreFileTypeListService.GetGitIgnoreFileTypes(cancellationToken);
        if (getGitIgnoreFileTypesResult.IsT1)
            return getGitIgnoreFileTypesResult.AsT1;

        return getGitIgnoreFileTypesResult.AsT0.Select(x => x.ToContractModel()).ToList();
    }
}