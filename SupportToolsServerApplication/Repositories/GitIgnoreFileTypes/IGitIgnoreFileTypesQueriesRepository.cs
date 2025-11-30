using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using RepositoriesDom;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;

public interface IGitIgnoreFileTypesQueriesRepository : IAbstractRepository
{
    //Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default);
    //Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default);
    //Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default);
    //Task<OneOf<GitDataDto, Err[]>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken);
    Task<OneOf<List<GitIgnoreFileTypeDto>, Err[]>> GetGitIgnoreFileTypes(CancellationToken cancellationToken);
    Task<OneOf<List<string>, Err[]>> GetGitIgnoreFileTypeNames(CancellationToken cancellationToken);
}