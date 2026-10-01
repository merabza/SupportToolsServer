using System.Collections.Generic;
using FluentValidation.Results;
using SupportToolsServer.Application.GitRepos.UpdateGitRepo;
using SupportToolsServer.Application.GitRepos.UploadGitRepos;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class GitRepoCommandValidatorsTests
{
    private static ValidationResult ValidateUpload(List<StsGitDataModel>? gits,
        List<StsGitIgnoreFileTypeDataModel>? gitIgnoreFiles)
    {
        return new UploadGitReposCommandValidator().Validate(new UploadGitReposCommand(gits!, gitIgnoreFiles!));
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void UpdateValidator_AcceptsAValidGit()
    {
        var command = new UpdateGitRepoCommand(TestData.GitModel("RepoA", "CSharp"));

        ValidationResult result = new UpdateGitRepoCommandValidator().Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateValidator_AppliesTheGitRules()
    {
        StsGitDataModel model = TestData.GitModel("RepoA", "CSharp");
        model.GitProjectFolderName = string.Empty;

        ValidationResult result = new UpdateGitRepoCommandValidator().Validate(new UpdateGitRepoCommand(model));

        AssertSingleError(result, "ValueRequired", "RepoA.GitProjectFolderName Is Required");
    }

    [Fact]
    public void UploadValidator_AcceptsValidLists()
    {
        ValidationResult result = ValidateUpload([TestData.GitModel("RepoA", "CSharp")],
            [TestData.GitIgnoreModel("CSharp")]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UploadValidator_AcceptsEmptyLists()
    {
        Assert.True(ValidateUpload([], []).IsValid);
    }

    [Fact]
    public void UploadValidator_RejectsAMissingGitList()
    {
        AssertSingleError(ValidateUpload(null, []), "ValueRequired", "Gits Is Required");
    }

    [Fact]
    public void UploadValidator_RejectsAMissingGitIgnoreFileList()
    {
        AssertSingleError(ValidateUpload([], null), "ValueRequired", "GitIgnoreFiles Is Required");
    }

    [Fact]
    public void UploadValidator_AppliesTheGitRulesToEveryGit()
    {
        StsGitDataModel invalid = TestData.GitModel("RepoB", "CSharp");
        invalid.GitProjectFolderName = string.Empty;

        ValidationResult result = ValidateUpload([TestData.GitModel("RepoA", "CSharp"), invalid], []);

        AssertSingleError(result, "ValueRequired", "RepoB.GitProjectFolderName Is Required");
    }

    [Fact]
    public void UploadValidator_AppliesTheGitIgnoreFileRulesToEveryFile()
    {
        StsGitIgnoreFileTypeDataModel invalid = TestData.GitIgnoreModel("React");
        invalid.Content = null!;

        ValidationResult result = ValidateUpload([], [TestData.GitIgnoreModel("CSharp"), invalid]);

        AssertSingleError(result, "ValueRequired", "React.Content Is Required");
    }

    [Fact]
    public void UploadValidator_RejectsGitNamesThatDifferOnlyInCase()
    {
        ValidationResult result = ValidateUpload(
            [TestData.GitModel("RepoA", "CSharp"), TestData.GitModel("repoa", "CSharp", "git@github.com:test/b.git")],
            []);

        AssertSingleError(result, "ValuesNotUnique", "GitProjectName Values Are Not Unique");
    }

    [Fact]
    public void UploadValidator_RejectsARepeatedAddress()
    {
        ValidationResult result = ValidateUpload([
            TestData.GitModel("RepoA", "CSharp", "git@github.com:test/a.git"),
            TestData.GitModel("RepoB", "CSharp", "git@github.com:test/a.git")
        ], []);

        AssertSingleError(result, "ValuesNotUnique", "GitProjectAddress Values Are Not Unique");
    }

    [Fact]
    public void UploadValidator_RejectsARepeatedGitIgnoreFileName()
    {
        ValidationResult result =
            ValidateUpload([], [TestData.GitIgnoreModel("CSharp"), TestData.GitIgnoreModel("CSHARP")]);

        AssertSingleError(result, "ValuesNotUnique", "Name Values Are Not Unique");
    }
}
