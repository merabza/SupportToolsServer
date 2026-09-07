using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.List;

public class GitIgnoreFileTypeListService : IScopedServiceSupportToolsServerApplication
{
    private readonly IGitIgnoreFileTypesQueriesRepository _gitsRepo;

    public GitIgnoreFileTypeListService(IGitIgnoreFileTypesQueriesRepository gitsRepo)
    {
        _gitsRepo = gitsRepo;
    }

    public async Task<Result<List<GitIgnoreFileTypeDto>>> GetGitIgnoreFileTypes(CancellationToken cancellationToken)
    {
        Result<List<GitIgnoreFileTypeDto>> gitIgnoreFileTypes =
            await _gitsRepo.GetGitIgnoreFileTypes(cancellationToken);
        return gitIgnoreFileTypes;
    }

    public async Task<Result<List<string>>> GetGitIgnoreFileTypeNames(CancellationToken cancellationToken)
    {
        Result<List<string>> gitIgnoreFileTypeNames = await _gitsRepo.GetGitIgnoreFileTypeNames(cancellationToken);
        return gitIgnoreFileTypeNames;
    }
}
