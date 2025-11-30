using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemToolsShared.Errors;
using WebInstallers;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.List;

public class GitIgnoreFileTypeListService : IScopedService
{
    private readonly IGitIgnoreFileTypesQueriesRepository _gitsRepo;

    public GitIgnoreFileTypeListService(IGitIgnoreFileTypesQueriesRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public async Task<OneOf<List<GitIgnoreFileTypeDto>, Err[]>> GetGitIgnoreFileTypes(
        CancellationToken cancellationToken)
    {
        var gitIgnoreFileTypes = await _gitsRepo.GetGitIgnoreFileTypes(cancellationToken);
        return gitIgnoreFileTypes;
    }

    public async Task<OneOf<List<string>, Err[]>> GetGitIgnoreFileTypeNames(CancellationToken cancellationToken)
    {
        var gitIgnoreFileTypeNames = await _gitsRepo.GetGitIgnoreFileTypeNames(cancellationToken);
        return gitIgnoreFileTypeNames;
    }
}