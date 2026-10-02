using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitRepos;

namespace SupportToolsServer.Application.GitRepos;

internal static class GitRepoContractMapper
{
    //კონტრაქტში gitignore შაბლონი სახელით გადაიცემა, ბაზაში კი მისი Id ინახება
    public static StsGitDataModel ToContractModel(this GitRepo gitRepo, string gitIgnorePatternName)
    {
        return new StsGitDataModel
        {
            GitProjectName = gitRepo.Name,
            GitProjectAddress = gitRepo.Address,
            GitProjectFolderName = gitRepo.FolderName,
            GitIgnorePatternName = gitIgnorePatternName,
            Version = gitRepo.Version
        };
    }
}
