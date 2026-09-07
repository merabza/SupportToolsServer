using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApplication.Services.Gits.Models;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Repositories.Gits;

public interface IGitsCommandsRepository
{
    //Task AddGit(GitData gitData, CancellationToken cancellationToken = default);
    Task<Result<int>> UpdateGitRepo(GitDataForSave requestNewRecord, CancellationToken cancellationToken);

    Task DeleteGitRepo(string gitKey, CancellationToken cancellationToken);
}
