using System.Collections.Generic;
using SupportToolsServer.Application.GitRepos;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class GitRepoContractMapperTests
{
    [Fact]
    public void ToContractModel_CopiesTheGitAndTheGivenPatternName()
    {
        GitIgnoreFileType cSharp = TestData.NewGitIgnoreFileType("CSharp");
        var gitRepo = new GitRepo(GitRepoId.CreateUnique(), "RepoA", "git@github.com:test/a.git", "FolderA", cSharp.Id,
            3);

        StsGitDataModel model = gitRepo.ToContractModel("CSharp");

        Assert.Equal("RepoA", model.GitProjectName);
        Assert.Equal("git@github.com:test/a.git", model.GitProjectAddress);
        Assert.Equal("FolderA", model.GitProjectFolderName);
        Assert.Equal("CSharp", model.GitIgnorePatternName);
        Assert.Equal(3, model.Version);
    }

    //Projects store a git by its id and give its name in the contract
    [Fact]
    public void ToNamesById_GivesTheNameOfEveryGitById()
    {
        GitIgnoreFileType cSharp = TestData.NewGitIgnoreFileType("CSharp");
        GitRepo repoA = TestData.NewGitRepo("RepoA", cSharp);
        GitRepo repoB = TestData.NewGitRepo("RepoB", cSharp);

        Dictionary<GitRepoId, string> names = new[] { repoA, repoB }.ToNamesById();

        Assert.Equal(2, names.Count);
        Assert.Equal("RepoA", names[repoA.Id]);
        Assert.Equal("RepoB", names[repoB.Id]);
    }
}
