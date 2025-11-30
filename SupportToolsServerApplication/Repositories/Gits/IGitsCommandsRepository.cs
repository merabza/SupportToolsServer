using OneOf;
using SupportToolsServerApplication.Services.Gits.Models;
using System.Threading;
using System.Threading.Tasks;
using SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Repositories.Gits;

public interface IGitsCommandsRepository
{
    //Task AddGit(GitData gitData, CancellationToken cancellationToken = default);
    Task<OneOf<int, Err[]>> UpdateGitRepo(GitDataForSave requestNewRecord, CancellationToken cancellationToken);

    Task DeleteGitRepo(string key, CancellationToken cancellationToken);
}