using System.Collections.Generic;
using System.Linq;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitRepos;

namespace SupportToolsServer.Application.GitRepos;

internal static class GitRepoContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordIsInUse, ReferencedRecordsNotFound)
    public const string EntityName = "GitRepo";

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

    //სხვა აგრეგატები git-ს Id-ით ინახავენ, კონტრაქტში კი მის სახელს გადასცემენ (StsProjectDataModel.GitProjectNames)
    public static Dictionary<GitRepoId, string> ToNamesById(this IEnumerable<GitRepo> gitRepos)
    {
        return gitRepos.ToDictionary(x => x.Id, x => x.Name);
    }
}
