using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using RepositoriesDom;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerDb.Models;
using SystemToolsShared.Errors;

namespace SupportToolsServerDom;

public interface IGitsRepository : IAbstractRepository
{
    Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default);
    Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default);
    Task AddGitIgnorePath(GitIgnoreFileType newGitIgnorePath, CancellationToken cancellationToken = default);
    void UpdateGitIgnorePath(GitIgnoreFileType dbGitIgnorePath, CancellationToken cancellationToken = default);
    Task AddGit(GitData gitData, CancellationToken cancellationToken = default);
    Task<List<GitDataDto>> GetGitRepos(CancellationToken cancellationToken = default);
    Task<OneOf<GitDataDto, Err[]>> GetGitRepoByKey(string gitKey, CancellationToken cancellationToken);
}