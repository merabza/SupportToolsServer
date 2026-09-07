using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.Update;

public class GitIgnoreFileTypeUpdateService : IScopedServiceSupportToolsServerApplication
{
    private readonly IGitIgnoreFileTypesCommandsRepository _gitIgnoreFileTypesCommandsRepo;

    public GitIgnoreFileTypeUpdateService(IGitIgnoreFileTypesCommandsRepository gitIgnoreFileTypesCommandsRepo)
    {
        _gitIgnoreFileTypesCommandsRepo = gitIgnoreFileTypesCommandsRepo;
    }

    public async Task<Result<int>> UpdateGitIgnoreFileType(GitIgnoreFileTypeForSave requestGitIgnoreFileTypeModel,
        CancellationToken cancellationToken)
    {
        return await _gitIgnoreFileTypesCommandsRepo.UpdateGitIgnoreFileType(requestGitIgnoreFileTypeModel,
            cancellationToken);
    }
}
