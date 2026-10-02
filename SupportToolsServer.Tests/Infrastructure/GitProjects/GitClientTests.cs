using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Tests.TestInfrastructure;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.GitProjects;

//Runs the real git against local repositories: a bare origin, a clone of it under test and a second clone
//that pushes new commits to the origin
public sealed class GitClientTests : IDisposable
{
    private readonly string _clone;
    private readonly string _notARepository;
    private readonly string _origin;
    private readonly GitClient _sut = new(NullLogger<GitClient>.Instance);
    private readonly TempFolder _temp = new();
    private readonly string _upstream;

    public GitClientTests()
    {
        _origin = _temp.Combine("origin.git");
        _upstream = _temp.Combine("upstream");
        _clone = _temp.Combine("clone");
        _notARepository = _temp.Combine("plain");
        Directory.CreateDirectory(_notARepository);

        GitRunner.Run(_temp.Path, $"init -q --bare -b main \"{_origin}\"");
        GitRunner.Run(_temp.Path, $"clone -q \"{_origin}\" \"{_upstream}\"");
        CommitFile(_upstream, "a.txt", "a");
        GitRunner.Run(_upstream, "push -q origin HEAD:main");
        GitRunner.Run(_temp.Path, $"clone -q \"{_origin}\" \"{_clone}\"");
    }

    public void Dispose()
    {
        _temp.Dispose();
    }

    private static void CommitFile(string repository, string fileName, string content)
    {
        File.WriteAllText(Path.Combine(repository, fileName), content);
        GitRunner.Run(repository, "add .");
        GitRunner.Run(repository, $"commit -q -m {fileName}");
    }

    private void PushNewCommitToOrigin()
    {
        CommitFile(_upstream, "b.txt", "b");
        GitRunner.Run(_upstream, "push -q origin HEAD:main");
    }

    private bool NeedPullAfterRemoteUpdate()
    {
        Assert.True(_sut.RemoteUpdate(_clone).IsSuccess);
        return _sut.NeedPull(_clone).Value;
    }

    [Fact]
    public void GetRemoteOriginUrl_ReturnsTheAddressTheFolderWasClonedFrom()
    {
        Result<string> result = _sut.GetRemoteOriginUrl(_clone);

        Assert.Equal(_origin, result.Value);
    }

    [Fact]
    public void GetRemoteOriginUrl_Fails_WhenTheFolderIsNotARepository()
    {
        Assert.True(_sut.GetRemoteOriginUrl(_notARepository).IsFailure);
    }

    [Fact]
    public void Clone_ClonesTheAddressIntoTheFolder()
    {
        string folder = _temp.Combine("Gits", "new clone");

        Result result = _sut.Clone(_origin, folder);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(folder, "a.txt")));
    }

    [Fact]
    public void Clone_Fails_WhenTheAddressIsNotARepository()
    {
        Result result = _sut.Clone(_temp.Combine("missing.git"), _temp.Combine("failed"));

        Assert.True(result.IsFailure);
        Assert.Equal("RunProcessError", result.Error.Code);
    }

    [Fact]
    public void HasChanges_ReturnsFalse_ForACleanClone()
    {
        Assert.False(_sut.HasChanges(_clone).Value);
    }

    [Fact]
    public void HasChanges_ReturnsTrue_WhenAFileWasAdded()
    {
        File.WriteAllText(Path.Combine(_clone, "new.txt"), "new");

        Assert.True(_sut.HasChanges(_clone).Value);
    }

    [Fact]
    public void HasChanges_Fails_WhenTheFolderIsNotARepository()
    {
        Assert.True(_sut.HasChanges(_notARepository).IsFailure);
    }

    [Fact]
    public void Restore_RevertsChangedFilesAndRemovesNewOnes()
    {
        File.WriteAllText(Path.Combine(_clone, "a.txt"), "changed");
        File.WriteAllText(Path.Combine(_clone, "new.txt"), "new");
        GitRunner.Run(_clone, "add a.txt");

        Result result = _sut.Restore(_clone);

        Assert.True(result.IsSuccess);
        Assert.Equal("a", File.ReadAllText(Path.Combine(_clone, "a.txt")));
        Assert.False(File.Exists(Path.Combine(_clone, "new.txt")));
        Assert.False(_sut.HasChanges(_clone).Value);
    }

    [Fact]
    public void Restore_Fails_WhenTheFolderIsNotARepository()
    {
        Assert.True(_sut.Restore(_notARepository).IsFailure);
    }

    [Fact]
    public void RemoteUpdate_Fails_WhenTheFolderIsNotARepository()
    {
        Assert.True(_sut.RemoteUpdate(_notARepository).IsFailure);
    }

    [Fact]
    public void NeedPull_ReturnsFalse_WhenTheCloneIsUpToDate()
    {
        Assert.False(NeedPullAfterRemoteUpdate());
    }

    [Fact]
    public void NeedPull_ReturnsTrue_WhenTheOriginHasNewCommits()
    {
        PushNewCommitToOrigin();

        Assert.True(NeedPullAfterRemoteUpdate());
    }

    [Fact]
    public void NeedPull_ReturnsFalse_WhenOnlyTheCloneHasNewCommits()
    {
        CommitFile(_clone, "local.txt", "local");

        Assert.False(NeedPullAfterRemoteUpdate());
    }

    [Fact]
    public void NeedPull_ReturnsTrue_WhenTheCloneAndTheOriginDiverged()
    {
        PushNewCommitToOrigin();
        CommitFile(_clone, "local.txt", "local");

        Assert.True(NeedPullAfterRemoteUpdate());
    }

    //Each failing step returns its own error, so the description names the git command that failed
    [Fact]
    public void NeedPull_ReturnsTheLocalIdError_WhenTheFolderIsNotARepository()
    {
        Result<bool> result = _sut.NeedPull(_notARepository);

        Assert.Contains(" rev-parse @ process", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void NeedPull_ReturnsTheRemoteIdError_WhenTheBranchHasNoUpstream()
    {
        GitRunner.Run(_clone, "checkout -q -b local");

        Result<bool> result = _sut.NeedPull(_clone);

        Assert.Contains(" rev-parse @{u} process", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void NeedPull_ReturnsTheBaseIdError_WhenTheBranchHasNoCommonBaseWithItsUpstream()
    {
        GitRunner.Run(_clone, "checkout -q --orphan unrelated");
        CommitFile(_clone, "unrelated.txt", "unrelated");
        GitRunner.Run(_clone, "branch -q --set-upstream-to=origin/main");

        Result<bool> result = _sut.NeedPull(_clone);

        Assert.Contains(" merge-base @ @{u} process", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void GitCommands_AreLoggedThroughTheInjectedLogger()
    {
        var logger = new CollectingLogger<GitClient>();
        var sut = new GitClient(logger);

        sut.GetRemoteOriginUrl(_clone);

        Assert.Contains(logger.Entries,
            e => e.Message.Contains("config --get remote.origin.url", StringComparison.Ordinal));
    }

    [Fact]
    public void GitCommands_LogTheCommandLineBeforeRunningIt()
    {
        var logger = new CollectingLogger<GitClient>();

        new GitClient(logger).GetRemoteOriginUrl(_clone);

        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Information &&
                 e.Message == $"Running git -C {_clone} config --get remote.origin.url");
    }

    [Fact]
    public void GitCommands_LogTheOutputOfASuccessfulCommand()
    {
        var logger = new CollectingLogger<GitClient>();

        new GitClient(logger).GetRemoteOriginUrl(_clone);

        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Information && e.Message.StartsWith(
                $"Output for 'git -C {_clone} config --get remote.origin.url' is{Environment.NewLine}{_origin}",
                StringComparison.Ordinal));
    }

    //git config writes nothing to stderr
    [Fact]
    public void GitCommands_DoNotLogAnEmptyErrorOutput()
    {
        var logger = new CollectingLogger<GitClient>();

        new GitClient(logger).GetRemoteOriginUrl(_clone);

        Assert.DoesNotContain(logger.Entries, e => e.Message.StartsWith("Error output", StringComparison.Ordinal));
    }

    //git clone reports "Cloning into ..." on stderr even when it succeeds
    [Fact]
    public void GitCommands_LogTheErrorOutputOfASuccessfulCommand()
    {
        var logger = new CollectingLogger<GitClient>();
        string folder = _temp.Combine("logged clone");

        Result result = new GitClient(logger).Clone(_origin, folder);

        Assert.True(result.IsSuccess);
        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Information && e.Message.StartsWith(
                $"Error output for 'git clone -- {_origin} {folder}' is{Environment.NewLine}Cloning into",
                StringComparison.Ordinal));
    }

    [Fact]
    public void GitCommands_DescribeAFailureWithItsTrimmedErrorOutput()
    {
        Result<bool> result = _sut.HasChanges(_notARepository);

        Assert.StartsWith(
            $"RunProcessError: git -C {_notARepository} status --porcelain process was finished with errors. ExitCode=128{Environment.NewLine}fatal: not a git repository",
            result.Error.Description, StringComparison.Ordinal);
        Assert.False(char.IsWhiteSpace(result.Error.Description[^1]));
    }

    //Outside a repository "git config --get" fails with exit code 1 and writes nothing to stderr
    [Fact]
    public void GitCommands_DescribeAFailureWithoutErrorOutputByItsExitCode()
    {
        Result<string> result = _sut.GetRemoteOriginUrl(_notARepository);

        Assert.Equal(
            $"RunProcessError: git -C {_notARepository} config --get remote.origin.url process was finished with errors. ExitCode=1",
            result.Error.Description);
    }

    [Fact]
    public void GitCommands_LogAFailureAsAnError()
    {
        var logger = new CollectingLogger<GitClient>();

        new GitClient(logger).HasChanges(_notARepository);

        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Error && e.Message.StartsWith(
                $"git -C {_notARepository} status --porcelain process was finished with errors. ExitCode=128",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Pull_BringsTheNewCommitsOfTheOrigin()
    {
        PushNewCommitToOrigin();

        Result result = _sut.Pull(_clone);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(_clone, "b.txt")));
    }

    [Fact]
    public void Pull_Fails_WhenTheFolderIsNotARepository()
    {
        Assert.True(_sut.Pull(_notARepository).IsFailure);
    }
}
