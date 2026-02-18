using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemTools.SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.List;

public class GitIgnoreFileTypeListService : IScopedServiceSupportToolsServerApplication
{
    private readonly IGitIgnoreFileTypesQueriesRepository _gitsRepo;

    public GitIgnoreFileTypeListService(IGitIgnoreFileTypesQueriesRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public async Task<OneOf<List<GitIgnoreFileTypeDto>, Err[]>> GetGitIgnoreFileTypes(
        CancellationToken cancellationToken)
    {
        OneOf<List<GitIgnoreFileTypeDto>, Err[]> gitIgnoreFileTypes =
            await _gitsRepo.GetGitIgnoreFileTypes(cancellationToken);
        return gitIgnoreFileTypes;
    }

    public async Task<OneOf<List<string>, Err[]>> GetGitIgnoreFileTypeNames(CancellationToken cancellationToken)
    {
        OneOf<List<string>, Err[]> gitIgnoreFileTypeNames =
            await _gitsRepo.GetGitIgnoreFileTypeNames(cancellationToken);
        return gitIgnoreFileTypeNames;
    }
}
