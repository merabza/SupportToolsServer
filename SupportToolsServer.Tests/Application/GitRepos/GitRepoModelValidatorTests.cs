using FluentValidation.Results;
using SupportToolsServer.Application.GitRepos;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class GitRepoModelValidatorTests
{
    private static ValidationResult Validate(StsGitDataModel model)
    {
        return new GitRepoModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    //A valid git@ address of exactly the given length
    private static string AddressOfLength(int length)
    {
        const string prefix = "git@github.com:test/";
        const string suffix = ".git";
        return prefix + new string('a', length - prefix.Length - suffix.Length) + suffix;
    }

    private static StsGitDataModel GitModelWithFolderName(string folderName)
    {
        StsGitDataModel model = TestData.GitModel("RepoA", "CSharp");
        model.GitProjectFolderName = folderName;
        return model;
    }

    [Fact]
    public void Validate_AcceptsAFilledModel()
    {
        Assert.True(Validate(TestData.GitModel("RepoA", "CSharp")).IsValid);
    }

    [Fact]
    public void Validate_AcceptsValuesOfTheMaximumLengths()
    {
        var model = new StsGitDataModel
        {
            GitProjectName = new string('n', 50),
            GitProjectAddress = AddressOfLength(256),
            GitProjectFolderName = new string('f', 100),
            GitIgnorePatternName = new string('p', 50)
        };

        Assert.True(Validate(model).IsValid);
    }

    [Fact]
    public void Validate_RejectsAMissingName()
    {
        StsGitDataModel model = TestData.GitModel(string.Empty, "CSharp", "git@github.com:test/a.git");
        model.GitProjectFolderName = "a";

        AssertSingleError(Validate(model), "ValueRequired", "GitProjectName Is Required");
    }

    [Fact]
    public void Validate_RejectsATooLongName()
    {
        StsGitDataModel model = TestData.GitModel(new string('n', 51), "CSharp", "git@github.com:test/a.git");

        AssertSingleError(Validate(model), "ValueTooLong", "GitProjectName Is Longer Than 50 Characters");
    }

    [Fact]
    public void Validate_RejectsAMissingAddress_NamingTheGit()
    {
        StsGitDataModel model = TestData.GitModel("RepoA", "CSharp");
        model.GitProjectAddress = string.Empty;

        AssertSingleError(Validate(model), "ValueRequired", "RepoA.GitProjectAddress Is Required");
    }

    [Fact]
    public void Validate_RejectsATooLongAddress_NamingTheGit()
    {
        StsGitDataModel model = TestData.GitModel("RepoA", "CSharp", AddressOfLength(257));

        AssertSingleError(Validate(model), "ValueTooLong", "RepoA.GitProjectAddress Is Longer Than 256 Characters");
    }

    [Fact]
    public void Validate_RejectsAMissingFolderName_NamingTheGit()
    {
        StsGitDataModel model = TestData.GitModel("RepoA", "CSharp");
        model.GitProjectFolderName = " ";

        AssertSingleError(Validate(model), "ValueRequired", "RepoA.GitProjectFolderName Is Required");
    }

    [Fact]
    public void Validate_RejectsATooLongFolderName_NamingTheGit()
    {
        StsGitDataModel model = TestData.GitModel("RepoA", "CSharp");
        model.GitProjectFolderName = new string('f', 101);

        AssertSingleError(Validate(model), "ValueTooLong", "RepoA.GitProjectFolderName Is Longer Than 100 Characters");
    }

    [Fact]
    public void Validate_RejectsAMissingGitIgnorePattern_NamingTheGit()
    {
        StsGitDataModel model = TestData.GitModel("RepoA", string.Empty);

        AssertSingleError(Validate(model), "ValueRequired", "RepoA.GitIgnorePatternName Is Required");
    }

    [Fact]
    public void Validate_RejectsATooLongGitIgnorePattern_NamingTheGit()
    {
        StsGitDataModel model = TestData.GitModel("RepoA", new string('p', 51));

        AssertSingleError(Validate(model), "ValueTooLong", "RepoA.GitIgnorePatternName Is Longer Than 50 Characters");
    }

    //The folder and address forms are covered by GitRulesTests; these tests check that the rules apply here

    [Fact]
    public void Validate_AcceptsAFolderNameRelativeToTheSpaProject()
    {
        Assert.True(Validate(GitModelWithFolderName(@"{SpaProjectFolderRelativePath}\src\carcass")).IsValid);
    }

    [Fact]
    public void Validate_RejectsAFolderNameThatIsNotARelativeFolderPath_NamingTheGit()
    {
        AssertSingleError(Validate(GitModelWithFolderName(@"..\..\x")), "InvalidGitFolderName",
            "RepoA.GitProjectFolderName Is Not A Valid Relative Folder Path");
    }

    [Fact]
    public void Validate_AcceptsAnSshUrlAddress()
    {
        Assert.True(Validate(TestData.GitModel("RepoA", "CSharp", "ssh://nas/volume1/GitServer/RepoA")).IsValid);
    }

    [Fact]
    public void Validate_RejectsAnAddressThatIsNotAGitAddress_NamingTheGit()
    {
        AssertSingleError(Validate(TestData.GitModel("RepoA", "CSharp", "--upload-pack=x")), "InvalidGitAddress",
            "RepoA.GitProjectAddress Is Not A Valid Git Address (git@host:path, ssh:// Or https://)");
    }

    [Fact]
    public void Validate_NamesOnlyTheField_WhenTheGitHasNoName()
    {
        StsGitDataModel model = TestData.GitModel(string.Empty, string.Empty, string.Empty);

        ValidationResult result = Validate(model);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "GitProjectAddress Is Required");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "GitIgnorePatternName Is Required");
    }
}
