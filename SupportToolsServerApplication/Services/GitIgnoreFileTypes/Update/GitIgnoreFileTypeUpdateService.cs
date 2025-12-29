using System.Threading;
using System.Threading.Tasks;
using OneOf;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.Update;

public class GitIgnoreFileTypeUpdateService : IScopedServiceSupportToolsServerApplication
{
    private readonly IGitIgnoreFileTypesCommandsRepository _gitIgnoreFileTypesCommandsRepo;

    public GitIgnoreFileTypeUpdateService(IGitIgnoreFileTypesCommandsRepository gitIgnoreFileTypesCommandsRepo)
    {
        _gitIgnoreFileTypesCommandsRepo = gitIgnoreFileTypesCommandsRepo;
    }

    public async Task<OneOf<int, Err[]>> UpdateGitIgnoreFileType(GitIgnoreFileTypeForSave requestGitIgnoreFileTypeModel,
        CancellationToken cancellationToken)
    {
        return await _gitIgnoreFileTypesCommandsRepo.UpdateGitIgnoreFileType(requestGitIgnoreFileTypeModel,
            cancellationToken);
    }
}