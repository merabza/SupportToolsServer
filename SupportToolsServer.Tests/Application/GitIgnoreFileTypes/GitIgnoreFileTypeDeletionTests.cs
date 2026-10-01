using SupportToolsServer.Application.GitIgnoreFileTypes;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitIgnoreFileTypes;

public sealed class GitIgnoreFileTypeDeletionTests
{
    private readonly GitIgnoreFileType _cSharp = TestData.NewGitIgnoreFileType("CSharp");
    private readonly GitIgnoreFileType _react = TestData.NewGitIgnoreFileType("React");

    [Fact]
    public void CheckNotUsed_Succeeds_WhenNothingIsToBeDeleted()
    {
        Result result = GitIgnoreFileTypeDeletion.CheckNotUsed([], [TestData.NewGitRepo("RepoA", _cSharp)]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckNotUsed_Succeeds_WhenNoGitUsesTheTypes()
    {
        Result result = GitIgnoreFileTypeDeletion.CheckNotUsed([_react], [TestData.NewGitRepo("RepoA", _cSharp)]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckNotUsed_ListsEveryUsedTypeWithItsGitsInNameOrder()
    {
        Result result = GitIgnoreFileTypeDeletion.CheckNotUsed([_cSharp, _react], [
            TestData.NewGitRepo("repoB", _cSharp), TestData.NewGitRepo("RepoC", _react),
            TestData.NewGitRepo("RepoA", _cSharp)
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal("GitIgnoreFileTypeIsInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("GitIgnore File Type Is Used By Gits: CSharp (RepoA, repoB); React (RepoC)",
            result.Error.Description);
    }

    [Fact]
    public void CheckNotUsed_LeavesOutTheUnusedTypes()
    {
        Result result = GitIgnoreFileTypeDeletion.CheckNotUsed([_cSharp, _react],
            [TestData.NewGitRepo("RepoC", _react)]);

        Assert.Equal("GitIgnore File Type Is Used By Gits: React (RepoC)", result.Error.Description);
    }
}
