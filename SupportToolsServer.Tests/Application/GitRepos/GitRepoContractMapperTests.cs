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
        var gitRepo = new GitRepo(GitRepoId.CreateUnique(), "RepoA", "git@github.com:test/a.git", "FolderA", cSharp.Id);

        StsGitDataModel model = gitRepo.ToContractModel("CSharp");

        Assert.Equal("RepoA", model.GitProjectName);
        Assert.Equal("git@github.com:test/a.git", model.GitProjectAddress);
        Assert.Equal("FolderA", model.GitProjectFolderName);
        Assert.Equal("CSharp", model.GitIgnorePatternName);
    }
}
