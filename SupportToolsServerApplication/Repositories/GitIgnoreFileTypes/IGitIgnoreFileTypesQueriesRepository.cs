using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;

public interface IGitIgnoreFileTypesQueriesRepository
{
    //Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default);
    //Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default);
    //Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default);
    //Task<Result<GitDataDto>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken);
    Task<Result<List<GitIgnoreFileTypeDto>>> GetGitIgnoreFileTypes(CancellationToken cancellationToken);
    Task<Result<List<string>>> GetGitIgnoreFileTypeNames(CancellationToken cancellationToken);
}
