using System.Collections.Generic;
using SupportToolsServerDb.Models;

namespace SupportToolsServerDom;

public interface IGitsRepository
{
    List<GitData> GetAllGitsFromDb();
    List<GitIgnoreFileType> GetAllGitIgnorePathsFromDb();
    void AddGitIgnorePath(GitIgnoreFileType newGitIgnorePath);
    void UpdateGitIgnorePath(GitIgnoreFileType dbGitIgnorePath);
    void AddGit(GitData gitData);
}