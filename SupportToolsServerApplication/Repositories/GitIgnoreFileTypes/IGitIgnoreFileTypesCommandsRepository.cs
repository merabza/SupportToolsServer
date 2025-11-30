using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;

namespace SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;

public interface IGitIgnoreFileTypesCommandsRepository
{
    //Task AddGitIgnorePath(GitIgnoreFileType newGitIgnorePath, CancellationToken cancellationToken = default);
    //void UpdateGitIgnorePath(GitIgnoreFileType dbGitIgnorePath, CancellationToken cancellationToken = default);
    //Task AddGit(GitData gitData, CancellationToken cancellationToken = default);
    //Task UpdateGitRepo(string gitKey, GitDataDto requestNewRecord, CancellationToken cancellationToken);
    //Task DeleteGitRepo(string gitKey, CancellationToken cancellationToken);

    //Task UpdateGitIgnoreFileType(GitIgnoreFileTypeDto requestNewRecord, CancellationToken cancellationToken);
    //Task AddGitIgnorePath(GitIgnoreFileType newGitIgnorePath, CancellationToken cancellationToken = default);
    Task<int> UpdateGitIgnoreFileType(GitIgnoreFileTypeForSave dbGitIgnorePath,
        CancellationToken cancellationToken = default);
}