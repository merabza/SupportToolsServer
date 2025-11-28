using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerDb.Models;

namespace SupportToolsServerDom;

public sealed class GitDataUpdater
{
    private readonly List<GitIgnoreFileDto> _gitIgnoreFiles;
    private readonly Dictionary<string, GitDataDto> _gits;
    private readonly IGitsQueriesRepository _gitsQueriesRepo;
    private readonly IGitsCommandsRepository _gitsCommandsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitDataUpdater(Dictionary<string, GitDataDto> gits, List<GitIgnoreFileDto> gitIgnoreFiles,
        IGitsQueriesRepository gitsQueriesRepo, IGitsCommandsRepository gitsCommandsRepo)
    {
        _gits = gits;
        _gitIgnoreFiles = gitIgnoreFiles;
        _gitsQueriesRepo = gitsQueriesRepo;
        _gitsCommandsRepo = gitsCommandsRepo;
    }

    public async Task Run(CancellationToken cancellationToken = default)
    {
        await SyncGitData(await SyncGitIgnoreFiles(cancellationToken), cancellationToken);
    }

    private async Task SyncGitData(List<GitIgnoreFileType> gitIgnoreFileTypes,
        CancellationToken cancellationToken = default)
    {
        var dbGits = await _gitsQueriesRepo.GetAllGitsFromDb(cancellationToken);

        foreach (var git in _gits)
        {
            var gitIgnoreFileType = gitIgnoreFileTypes.Find(f => f.Name == git.Value.GitIgnorePathName);
            if (gitIgnoreFileType is null)
                continue;
            var dbGitByAddress = dbGits.Find(g => g.GdGitAddress == git.Value.GitProjectAddress);
            var dbGitByName = dbGits.Find(g => g.GdGitAddress == git.Key);
            if (dbGitByAddress is null && dbGitByName is null)
            {
                var newGitData = new GitData
                {
                    GdName = git.Key,
                    GdGitAddress = git.Value.GitProjectAddress,
                    GitIgnoreFileTypeNavigation = gitIgnoreFileType,
                    GdFolderName = git.Value.GitProjectFolderName
                };
                await _gitsCommandsRepo.AddGit(newGitData, cancellationToken);
            }
            else if (dbGitByAddress is not null && dbGitByName is not null)
            {
                if (dbGitByAddress.GdId != dbGitByName.GdId)
                    continue;

                if (dbGitByName.GitIgnoreFileTypeId == gitIgnoreFileType.Id &&
                    dbGitByName.GdGitAddress == git.Value.GitProjectAddress)
                    continue;
                dbGitByName.GitIgnoreFileTypeNavigation = gitIgnoreFileType;
                dbGitByName.GdGitAddress = git.Value.GitProjectAddress;
            }
        }
        await _gitsQueriesRepo.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<GitIgnoreFileType>> SyncGitIgnoreFiles(CancellationToken cancellationToken = default)
    {
        var dbGitIgnorePaths = await _gitsQueriesRepo.GetAllGitIgnorePathsFromDb(cancellationToken);

        foreach (var gitIgnoreFile in _gitIgnoreFiles)
        {
            var dbGitIgnorePath = dbGitIgnorePaths.Find(g => g.Name == gitIgnoreFile.Name);
            if (dbGitIgnorePath is null)
            {
                var newGitIgnorePath = new GitIgnoreFileType
                {
                    Name = gitIgnoreFile.Name, Content = gitIgnoreFile.Content
                };
                await _gitsCommandsRepo.AddGitIgnorePath(newGitIgnorePath, cancellationToken);
                dbGitIgnorePaths.Add(newGitIgnorePath);
            }
            else
            {
                if (dbGitIgnorePath.Content == gitIgnoreFile.Content)
                    continue;
                dbGitIgnorePath.Content = gitIgnoreFile.Content;
                _gitsCommandsRepo.UpdateGitIgnorePath(dbGitIgnorePath, cancellationToken);
            }
        }

        return dbGitIgnorePaths;
    }

    //private async Task<List<GitIgnoreFileType>> SyncGitIgnoreFiles(CancellationToken cancellationToken = default)
    //{
    //    var dbGitIgnorePaths = await _gitsRepo.GetAllGitIgnorePathsFromDb(cancellationToken);

    //    foreach (var gitIgnoreFile in _gitIgnoreFiles)
    //    {
    //        var dbGitIgnorePath = dbGitIgnorePaths.Find(g => g.Name == gitIgnoreFile.Name);
    //        if (dbGitIgnorePath is null)
    //        {
    //            var newGitIgnorePath = new GitIgnoreFileType
    //            {
    //                Name = gitIgnoreFile.Name, Content = gitIgnoreFile.Content
    //            };
    //            await _gitsRepo.AddGitIgnorePath(newGitIgnorePath, cancellationToken);
    //            dbGitIgnorePaths.Add(newGitIgnorePath);
    //        }
    //        else
    //        {
    //            if (dbGitIgnorePath.Content == gitIgnoreFile.Content)
    //                continue;
    //            dbGitIgnorePath.Content = gitIgnoreFile.Content;
    //            _gitsRepo.UpdateGitIgnorePath(dbGitIgnorePath, cancellationToken);
    //        }
    //    }

    //    return dbGitIgnorePaths;
    //}
}