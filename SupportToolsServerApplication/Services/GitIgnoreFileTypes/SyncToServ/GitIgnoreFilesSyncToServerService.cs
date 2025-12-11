using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;
using SystemToolsShared.Errors;
using WebInstallers;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.SyncToServ;

// ReSharper disable once UnusedType.Global
public class GitIgnoreFilesSyncToServerService : IScopedService
{
    private readonly IGitIgnoreFileTypesCommandsRepository _gitIgnoreFileTypesCommandsRepo;

    public GitIgnoreFilesSyncToServerService(IGitIgnoreFileTypesCommandsRepository gitIgnoreFileTypesCommandsRepo)
    {
        _gitIgnoreFileTypesCommandsRepo = gitIgnoreFileTypesCommandsRepo;
    }

    public async Task<OneOf<Unit, Err[]>> SyncGitIgnoreFilesToServer(
        IEnumerable<GitIgnoreFileTypeForSave> requestGitIgnoreFiles, CancellationToken cancellationToken = default)
    {
        foreach (var gitIgnoreFile in requestGitIgnoreFiles)
            await _gitIgnoreFileTypesCommandsRepo.UpdateGitIgnoreFileType(gitIgnoreFile, cancellationToken);

        return new Unit();
    }
}