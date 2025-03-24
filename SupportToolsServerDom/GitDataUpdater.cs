using System;
using System.Collections.Generic;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerDb.Models;

namespace SupportToolsServerDom;

public class GitDataUpdater
{
    private readonly List<GitDataDomain> _gits;
    private readonly List<GitIgnoreFile> _gitIgnoreFiles;
    private readonly IGitsRepository _gitsRepo;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitDataUpdater(List<GitDataDomain> gits, List<GitIgnoreFile> GitIgnoreFiles, IGitsRepository gitsRepo)
    {
        _gits = gits;
        _gitIgnoreFiles = GitIgnoreFiles;
        _gitsRepo = gitsRepo;
    }

    public bool Run()
    {
        SyncGitData(SyncGitIgnoreFiles());


        return true;
    }

    private void SyncGitData(List<GitIgnoreFileType> gitIgnoreFileTypes)
    {
        var dbGits = _gitsRepo.GetAllGitsFromDb();

        foreach (var git in _gits)
        {
            var gitIgnoreFileType = gitIgnoreFileTypes.Find(f => f.Name == git.GitIgnorePathName);
            if ( gitIgnoreFileType is null)
                continue;
            var dbGit = dbGits.Find(g => g.GitAddress== git.GitProjectAddress);
            if (dbGit == null)
            {
                var newGitData = new GitData
                {
                    GitAddress = git.GitProjectAddress,
                    Name = git.GitProjectFolderName,
                    GitIgnoreFileTypeNavigation = gitIgnoreFileType
                };
                _gitsRepo.AddGit(newGitData);
            }
            else
            {
                if (dbGit.Name == git.GitIgnorePathName && dbGit.GitAddress == git.GitProjectFolderName)
                    continue;
                dbGit.GitProjectFolderName = git.GitProjectFolderName;
                dbGit.GitIgnorePathName = git.GitIgnorePathName;
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
                    Name = gitIgnoreFile.Name,
                    Content = gitIgnoreFile.Content,
                };
                _gitsRepo.AddGitIgnorePath(newGitIgnorePath);
                dbGitIgnorePaths.Add(newGitIgnorePath);
            }
            else
            {
                if ( dbGitIgnorePath.Content == gitIgnoreFile.Content)
                    continue;
                dbGitIgnorePath.Content = gitIgnoreFile.Content;
                _gitsRepo.UpdateGitIgnorePath(dbGitIgnorePath);
            }
        }

        return dbGitIgnorePaths;
    }

}