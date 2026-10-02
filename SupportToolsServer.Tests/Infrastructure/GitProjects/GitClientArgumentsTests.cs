using System.Collections.Generic;
using System.Diagnostics;
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
