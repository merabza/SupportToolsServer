using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;

namespace SupportToolsServer.Tests.TestInfrastructure;

internal static class TestData
{
    public static GitIgnoreFileType NewGitIgnoreFileType(string name, string content = "bin/")
    {
        return new GitIgnoreFileType(GitIgnoreFileTypeId.CreateUnique(), name, content);
    }

    public static GitRepo NewGitRepo(string name, GitIgnoreFileType gitIgnoreFileType, string? address = null)
    {
        return new GitRepo(GitRepoId.CreateUnique(), name, address ?? AddressOf(name), name, gitIgnoreFileType.Id);
    }

    public static StsGitDataModel GitModel(string name, string gitIgnorePatternName, string? address = null)
    {
        return new StsGitDataModel
        {
            GitProjectName = name,
            GitProjectAddress = address ?? AddressOf(name),
            GitProjectFolderName = name,
            GitIgnorePatternName = gitIgnorePatternName
        };
    }

    public static StsGitIgnoreFileTypeDataModel GitIgnoreModel(string name, string content = "bin/")
    {
        return new StsGitIgnoreFileTypeDataModel { Name = name, Content = content };
    }

    public static string AddressOf(string gitName)
    {
        return $"git@github.com:test/{gitName}.git";
    }
}
