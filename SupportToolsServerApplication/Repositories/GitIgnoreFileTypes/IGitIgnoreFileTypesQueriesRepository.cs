using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemTools.SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;

public interface IGitIgnoreFileTypesQueriesRepository
{
    //Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default);
    //Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default);
    //Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default);
    //Task<OneOf<GitDataDto, Error[]>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken);
    Task<OneOf<List<GitIgnoreFileTypeDto>, Error[]>> GetGitIgnoreFileTypes(CancellationToken cancellationToken);
    Task<OneOf<List<string>, Error[]>> GetGitIgnoreFileTypeNames(CancellationToken cancellationToken);
}
