using System.Collections.Generic;
using System.Diagnostics;
using SupportToolsServer.Application.GitRepos.GitRemoteHeads;
using SupportToolsServer.Infrastructure.GitProjects;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.GitProjects;

//The argument lists of the git commands, without running git: every value is a separate argument,
//so an address or a path with spaces or a leading dash can't become an option of its own
public sealed class GitClientArgumentsTests
{
    private const string ProjectPath = @"C:\Work\Gits\Repo A";

    private readonly List<string[]> _calls = [];

    //Every git command succeeds with the given output
    private GitClient CreateClient(string output)
    {
        return new GitClient(arguments =>
        {
            _calls.Add([.. arguments]);
            return output;
        });
    }

    [Fact]
    public void Clone_PassesTheAddressAndTheFolderAfterTheEndOfTheOptions()
    {
        Result result = CreateClient(string.Empty).Clone("--upload-pack=touch x", ProjectPath);

        Assert.True(result.IsSuccess);
        string[] expected = ["clone", "--", "--upload-pack=touch x", ProjectPath];
        Assert.Equal(expected, Assert.Single(_calls));
    }

    [Fact]
    public void FolderCommands_PassTheFolderAndEachArgumentSeparately()
    {
        GitClient sut = CreateClient("same output");

        sut.GetRemoteOriginUrl(ProjectPath);
        sut.HasChanges(ProjectPath);
        sut.Restore(ProjectPath);
        sut.RemoteUpdate(ProjectPath);
        sut.NeedPull(ProjectPath);
        sut.Pull(ProjectPath);

        string[][] expected =
        [
            ["-C", ProjectPath, "config", "--get", "remote.origin.url"],
            ["-C", ProjectPath, "status", "--porcelain"],
            ["-C", ProjectPath, "reset"],
            ["-C", ProjectPath, "checkout", "."],
            ["-C", ProjectPath, "clean", "-fdx"],
            ["-C", ProjectPath, "remote", "update"],
            ["-C", ProjectPath, "rev-parse", "@"],
            ["-C", ProjectPath, "rev-parse", "@{u}"],
            ["-C", ProjectPath, "merge-base", "@", "@{u}"],
            ["-C", ProjectPath, "pull"]
        ];
        Assert.Equal(expected, _calls);
    }

    [Fact]
    public void GetRemoteHead_PassesTheAddressAfterTheEndOfTheOptions()
    {
        CreateClient(string.Empty).GetRemoteHead("--upload-pack=touch x");

        string[] expected = ["ls-remote", "--symref", "--", "--upload-pack=touch x", "HEAD"];
        Assert.Equal(expected, Assert.Single(_calls));
    }

    //The output of git on Windows ends its lines with \r\n
    [Fact]
    public void GetRemoteHead_ReadsTheBranchAndTheCommitOfHead()
    {
        Result<GitRemoteHead> result = CreateClient(
                "ref: refs/heads/master\tHEAD\r\n1dfa466db703de783bc9929e9bd765f2c870a1f2\tHEAD\r\n")
            .GetRemoteHead("git@github.com:test/a.git");

        Assert.Equal(new GitRemoteHead("master", "1dfa466db703de783bc9929e9bd765f2c870a1f2"), result.Value);
    }

    //Only the two HEAD lines count: another ref after them changes neither the branch nor the commit
    [Fact]
    public void GetRemoteHead_SkipsTheLinesThatAreNotAboutHead()
    {
        Result<GitRemoteHead> result = CreateClient(
                "ref: refs/heads/main\tHEAD\n1dfa466db703de783bc9929e9bd765f2c870a1f2\tHEAD\n" +
                "2222222222222222222222222222222222222222\trefs/heads/main\nunexpected\n")
            .GetRemoteHead("git@github.com:test/a.git");

        Assert.Equal(new GitRemoteHead("main", "1dfa466db703de783bc9929e9bd765f2c870a1f2"), result.Value);
    }

    //An empty repository has no HEAD; a detached HEAD has no branch
    [Theory]
    [InlineData("")]
    [InlineData("1dfa466db703de783bc9929e9bd765f2c870a1f2\tHEAD\n")]
    [InlineData("ref: refs/heads/main\tHEAD\n")]
    public void GetRemoteHead_Fails_WhenTheBranchOrTheCommitIsMissing(string output)
    {
        Result<GitRemoteHead> result = CreateClient(output).GetRemoteHead("git@github.com:test/a.git");

        Assert.Equal(GitProjectsErrors.RemoteHeadIsNotFound, result.Error);
    }

    [Fact]
    public void CreateStartInfo_PassesTheArgumentsAsAListAndNotAsOneString()
    {
        string[] arguments = ["clone", "--", "git@github.com:test/a b.git", ProjectPath];

        ProcessStartInfo startInfo = GitClient.CreateStartInfo(arguments);

        Assert.Equal("git", startInfo.FileName);
        Assert.Equal(arguments, startInfo.ArgumentList);
        Assert.Empty(startInfo.Arguments);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.True(startInfo.CreateNoWindow);
    }
}
