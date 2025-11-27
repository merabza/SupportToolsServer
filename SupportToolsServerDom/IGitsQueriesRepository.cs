using System.Collections.Generic;
using OneOf;
using RepositoriesDom;
using SupportToolsServerApiContracts.Models;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerDb.Models;
using SystemToolsShared.Errors;

namespace SupportToolsServerDom;

public interface IGitsQueriesRepository: IAbstractRepository
{
    Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default);
    Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default);
    Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default);
    Task<OneOf<GitDataDto, Err[]>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken);
}