using System.Threading;
using System.Threading.Tasks;
using OneOf;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemTools.SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.Update;

public class GitIgnoreFileTypeUpdateService : IScopedServiceSupportToolsServerApplication
{
    private readonly IGitIgnoreFileTypesCommandsRepository _gitIgnoreFileTypesCommandsRepo;

    public GitIgnoreFileTypeUpdateService(IGitIgnoreFileTypesCommandsRepository gitIgnoreFileTypesCommandsRepo)
    {
        _gitIgnoreFileTypesCommandsRepo = gitIgnoreFileTypesCommandsRepo;
    }

    public async Task<OneOf<int, Error[]>> UpdateGitIgnoreFileType(GitIgnoreFileTypeForSave requestGitIgnoreFileTypeModel,
        CancellationToken cancellationToken)
    {
        return await _gitIgnoreFileTypesCommandsRepo.UpdateGitIgnoreFileType(requestGitIgnoreFileTypeModel,
            cancellationToken);
    }
}
