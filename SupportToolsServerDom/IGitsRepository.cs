using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RepositoriesDom;
using SupportToolsServerDb.Models;

namespace SupportToolsServerDom;

public interface IGitsRepository : IAbstractRepository
{
    Task<List<GitData>> GetAllGitsFromDb(CancellationToken cancellationToken = default);
    Task<List<GitIgnoreFileType>> GetAllGitIgnorePathsFromDb(CancellationToken cancellationToken = default);
    Task AddGitIgnorePath(GitIgnoreFileType newGitIgnorePath, CancellationToken cancellationToken = default);
    void UpdateGitIgnorePath(GitIgnoreFileType dbGitIgnorePath, CancellationToken cancellationToken = default);
    Task AddGit(GitData gitData, CancellationToken cancellationToken = default);
}