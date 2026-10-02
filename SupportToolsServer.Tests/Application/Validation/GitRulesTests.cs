using FluentValidation;
using FluentValidation.Results;
using SupportToolsServer.Application.Validation;
using Xunit;

namespace SupportToolsServer.Tests.Application.Validation;

public sealed class GitRulesTests
{
    private const string ValidFolderName = "RepoA";
    private const string ValidAddress = "git@github.com:test/RepoA.git";

    private static ValidationResult Validate(string? folderName, string? address)
    {
        return new SampleValidator().Validate(new Sample("Owner", folderName, address));
    }

    [Theory]
    [InlineData("RepoA")]
    [InlineData("My Repo.v2")]
    [InlineData(@"Group\RepoA")]
    [InlineData("Group/Sub/RepoA")]
    [InlineData(@"{SpaProjectFolderRelativePath}\src\carcass")]
    [InlineData("{SpaProjectFolderRelativePath}/src")]
    [InlineData("{SpaProjectFolderRelativePath}src")]
    public void IsValidFolderName_AcceptsARelativeFolderPath(string folderName)
    {
        Assert.True(GitRules.IsValidFolderName(folderName));
    }

    [Theory]
    [InlineData(@"..\..\x")]
    [InlineData("a/../../b")]
    [InlineData(@"C:\Windows")]
    [InlineData("C:")]
    [InlineData(@"\\srv\share")]
    [InlineData("/etc")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData(@"x\.\y")]
    [InlineData(@"a\\b")]
    [InlineData("a//b")]
    [InlineData(@"a\")]
    [InlineData("...")]
    [InlineData(".. ")]
    [InlineData("a.")]
    [InlineData(@"a \b")]
    [InlineData("a:b")]
    [InlineData("a*b")]
    [InlineData("a?b")]
    [InlineData("a\"b")]
    [InlineData("a<b")]
    [InlineData("a>b")]
    [InlineData("a|b")]
    [InlineData("a\tb")]
    [InlineData("{SpaProjectFolderRelativePath}")]
    [InlineData(@"{SpaProjectFolderRelativePath}\")]
    [InlineData(@"{SpaProjectFolderRelativePath}\..\x")]
    [InlineData(@"{SpaProjectFolderRelativePath}\\x")]
    public void IsValidFolderName_RejectsAFolderNameThatIsNotARelativeFolderPath(string folderName)
    {
        Assert.False(GitRules.IsValidFolderName(folderName));
    }

    [Theory]
    [InlineData("git@github.com:merabza/X.git")]
    [InlineData("https://github.com/merabza/X.git")]
    [InlineData("ssh://nas/volume1/GitServer/RepoA")]
    [InlineData("ssh://git@git.example.com:2222/team/RepoA.git")]
    [InlineData("https://user@git.example.com:8443/team/RepoA.git")]
    public void IsValidAddress_AcceptsAGitAddress(string address)
    {
        Assert.True(GitRules.IsValidAddress(address));
    }

    [Theory]
    [InlineData("-oProxyCommand=x")]
    [InlineData("--upload-pack=x")]
    [InlineData("file:///c/x")]
    [InlineData("ext::sh -c x")]
    [InlineData("git@github.com:merabza/X Y.git")]
    [InlineData("git@github.com:merabza/X.git\n")]
    [InlineData("git@github.com:merabza/X\u0007.git")]
    [InlineData(@"C:\Repos\RepoA")]
    [InlineData(@"\\srv\share\RepoA")]
    [InlineData("/srv/git/RepoA")]
    [InlineData("../RepoA")]
    [InlineData("RepoA")]
    [InlineData("http://github.com/merabza/X.git")]
    [InlineData("user@github.com:merabza/X.git")]
    [InlineData("git@-oProxyCommand=x:y")]
    [InlineData("ssh://-oProxyCommand=x/y")]
    [InlineData("ssh://-oProxyCommand=x@nas/y")]
    [InlineData("https://-x/y")]
    [InlineData("git@github.com:")]
    [InlineData("ssh://nas")]
    [InlineData("https://github.com")]
    public void IsValidAddress_RejectsAnAddressThatIsNotAGitAddress(string address)
    {
        Assert.False(GitRules.IsValidAddress(address));
    }

    [Fact]
    public void ValidGitFolderName_AcceptsAValidFolderName()
    {
        Assert.True(Validate(ValidFolderName, ValidAddress).IsValid);
    }

    [Fact]
    public void ValidGitFolderName_RejectsAnInvalidFolderName_NamingTheValue()
    {
        ValidationFailure failure = Assert.Single(Validate(@"..\RepoA", ValidAddress).Errors);

        Assert.Equal("InvalidGitFolderName", failure.ErrorCode);
        Assert.Equal("Owner.FolderName Is Not A Valid Relative Folder Path", failure.ErrorMessage);
    }

    //A missing value is reported by RequiredWithMaxLength, not a second time by this rule
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidGitFolderName_LeavesAMissingValueToTheRequiredRule(string? folderName)
    {
        Assert.True(Validate(folderName, ValidAddress).IsValid);
    }

    [Fact]
    public void ValidGitAddress_RejectsAnInvalidAddress_NamingTheValue()
    {
        ValidationFailure failure = Assert.Single(Validate(ValidFolderName, "file:///c/x").Errors);

        Assert.Equal("InvalidGitAddress", failure.ErrorCode);
        Assert.Equal("Owner.Address Is Not A Valid Git Address (git@host:path, ssh:// Or https://)",
            failure.ErrorMessage);
    }

    //A missing value is reported by RequiredWithMaxLength, not a second time by this rule
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidGitAddress_LeavesAMissingValueToTheRequiredRule(string? address)
    {
        Assert.True(Validate(ValidFolderName, address).IsValid);
    }

    private sealed record Sample(string Owner, string? FolderName, string? Address);

    private sealed class SampleValidator : AbstractValidator<Sample>
    {
        public SampleValidator()
        {
            RuleFor(x => x.FolderName!).ValidGitFolderName(x => $"{x.Owner}.FolderName");
            RuleFor(x => x.Address!).ValidGitAddress(x => $"{x.Owner}.Address");
        }
    }
}
