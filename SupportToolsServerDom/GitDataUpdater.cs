using System.Collections.Generic;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerDb.Models;

namespace SupportToolsServerDom;

public class GitDataUpdater
{
    private readonly List<GitIgnoreFile> _gitIgnoreFiles;
    private readonly Dictionary<string, GitDataDomain> _gits;
    private readonly IGitsRepository _gitsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitDataUpdater(Dictionary<string, GitDataDomain> gits, List<GitIgnoreFile> GitIgnoreFiles,
        IGitsRepository gitsRepo)
    {
        _gits = gits;
        _gitIgnoreFiles = GitIgnoreFiles;
        _gitsRepo = gitsRepo;
    }

    public Task Run()
    {
        SyncGitData(SyncGitIgnoreFiles());
        return Task.CompletedTask;
    }

    private void SyncGitData(List<GitIgnoreFileType> gitIgnoreFileTypes)
    {
        var dbGits = _gitsRepo.GetAllGitsFromDb();

        foreach (var git in _gits)
        {
            var gitIgnoreFileType = gitIgnoreFileTypes.Find(f => f.Name == git.Value.GitIgnorePathName);
            if (gitIgnoreFileType is null)
                continue;
            var dbGitByAddress = dbGits.Find(g => g.GitAddress == git.Value.GitProjectAddress);
            var dbGitByName = dbGits.Find(g => g.GitAddress == git.Key);
            if (dbGitByAddress is null && dbGitByName is null)
            {
                var newGitData = new GitData
                {
                    Name = git.Key,
                    GitAddress = git.Value.GitProjectAddress,
                    GitIgnoreFileTypeNavigation = gitIgnoreFileType
                };
                _gitsRepo.AddGit(newGitData);
            }
            else if (dbGitByAddress is not null && dbGitByName is not null)
            {
                if (dbGitByAddress.Id != dbGitByName.Id)
                    continue;

                if (dbGitByName.GitIgnoreFileTypeId == gitIgnoreFileType.Id &&
                    dbGitByName.GitAddress == git.Value.GitProjectAddress)
                    continue;
                dbGitByName.GitIgnoreFileTypeNavigation = gitIgnoreFileType;
                dbGitByName.GitAddress = git.Value.GitProjectAddress;
            }
        }
    }

    private List<GitIgnoreFileType> SyncGitIgnoreFiles()
    {
        var dbGitIgnorePaths = _gitsRepo.GetAllGitIgnorePathsFromDb();

        foreach (var gitIgnoreFile in _gitIgnoreFiles)
        {
            var dbGitIgnorePath = dbGitIgnorePaths.Find(g => g.Name == gitIgnoreFile.Name);
            if (dbGitIgnorePath == null)
            {
                var newGitIgnorePath = new GitIgnoreFileType
                {
                    Name = gitIgnoreFile.Name, Content = gitIgnoreFile.Content
                };
                _gitsRepo.AddGitIgnorePath(newGitIgnorePath);
                dbGitIgnorePaths.Add(newGitIgnorePath);
            }
            else
            {
                if (dbGitIgnorePath.Content == gitIgnoreFile.Content)
                    continue;
                dbGitIgnorePath.Content = gitIgnoreFile.Content;
                _gitsRepo.UpdateGitIgnorePath(dbGitIgnorePath);
            }
        }

        return dbGitIgnorePaths;
    }
}