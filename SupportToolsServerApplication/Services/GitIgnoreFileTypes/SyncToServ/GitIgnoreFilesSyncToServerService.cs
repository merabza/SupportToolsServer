using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.SyncToServ;

// ReSharper disable once UnusedType.Global
public class GitIgnoreFilesSyncToServerService : IScopedServiceSupportToolsServerApplication
{
    private readonly IGitIgnoreFileTypesCommandsRepository _gitIgnoreFileTypesCommandsRepo;

    public GitIgnoreFilesSyncToServerService(IGitIgnoreFileTypesCommandsRepository gitIgnoreFileTypesCommandsRepo)
    {
        _gitIgnoreFileTypesCommandsRepo = gitIgnoreFileTypesCommandsRepo;
    }

    public async Task<Result> SyncGitIgnoreFilesToServer(IEnumerable<GitIgnoreFileTypeForSave> requestGitIgnoreFiles,
        CancellationToken cancellationToken = default)
    {
        foreach (GitIgnoreFileTypeForSave gitIgnoreFile in requestGitIgnoreFiles)
        {
            await _gitIgnoreFileTypesCommandsRepo.UpdateGitIgnoreFileType(gitIgnoreFile, cancellationToken);
        }

        return Result.Success();
    }
}
