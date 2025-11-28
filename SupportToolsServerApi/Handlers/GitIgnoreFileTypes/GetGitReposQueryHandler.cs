using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatRMessagingAbstractions;
using OneOf;
using SupportToolsServerDom;
using SystemToolsShared.Errors;

namespace SupportToolsServerApi.Handlers.GitIgnoreFileTypes;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class GetGitIgnoreFileTypesQueryHandler : IQueryHandler<GetGitIgnoreFileTypesQueryRequest, List<string>>
{
    private readonly IGitIgnoreFileTypesQueriesRepository _gitsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GetGitIgnoreFileTypesQueryHandler(IGitIgnoreFileTypesQueriesRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public async Task<OneOf<List<string>, Err[]>> Handle(GetGitIgnoreFileTypesQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        //ჩაიტვირთოს დერივაციის ფორმულები, ყველა
        return await _gitsRepo.GetGitIgnoreFileTypes(cancellationToken);
    }
}