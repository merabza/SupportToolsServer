using FluentValidation;
using FluentValidation.Results;
using SupportToolsServer.Application.Validation;
using Xunit;

namespace SupportToolsServer.Tests.Application.Validation;

public sealed class PathRulesTests
{
    [Theory]
    [InlineData(@"D:\1WorkSecurity\AppA\PAZISI\Prod\appsettings.json")]
    [InlineData(@"d:\a")]
    [InlineData(@"C:\a.b\.gitignore")]
    [InlineData(@"D:\1WorkSecurity\My App\app settings (1).json")]
    [InlineData(@"D:\a b+c&d=e#f%g;h,i'j[k]l{m}n~o@p$q.json")]
    [InlineData(@"D:\ქართული\ფაილი.json")]
    [InlineData(@"D:\ leading space")]
    public void IsValidAbsoluteFilePath_AcceptsAnAbsoluteWindowsFilePath(string path)
    {
        Assert.True(PathRules.IsValidAbsoluteFilePath(path));
    }

    //Relative and rooted forms other than X:\, other separators, empty segments, the names that Windows trims
    //(ending with . or a space, so also . and ..), the forbidden characters and the control characters
    [Theory]
    [InlineData("")]
    [InlineData("D")]
    [InlineData("D:")]
    [InlineData(@"D:\")]
    [InlineData("D:a.json")]
    [InlineData("D:/a.json")]
    [InlineData(@"1:\a.json")]
    [InlineData(@"ა:\a.json")]
    [InlineData(@":\a.json")]
    [InlineData(@"DD:\a.json")]
    [InlineData(@"\\server\share\a.json")]
    [InlineData(@"\\?\D:\a.json")]
    [InlineData(@"\a.json")]
    [InlineData(@"a\b.json")]
    [InlineData("/home/merab/a.json")]
    [InlineData(@"D:\a/b.json")]
    [InlineData(@"D:\a\\b.json")]
    [InlineData(@"D:\a\")]
    [InlineData(@"D:\a\..\b.json")]
    [InlineData(@"D:\..\b.json")]
    [InlineData(@"D:\a\.\b.json")]
    [InlineData(@"D:\a\...\b.json")]
    [InlineData(@"D:\a\.. \b.json")]
    [InlineData(@"D:\a.")]
    [InlineData(@"D:\a ")]
    [InlineData(@"D:\a\b:c.json")]
    [InlineData(@"D:\a*.json")]
    [InlineData(@"D:\a?.json")]
    [InlineData("D:\\a\".json")]
    [InlineData(@"D:\a<b.json")]
    [InlineData(@"D:\a>b.json")]
    [InlineData(@"D:\a|b.json")]
    [InlineData("D:\\a\tb.json")]
    [InlineData("D:\\a\nb.json")]
    [InlineData("D:\\a\0.json")]
    public void IsValidAbsoluteFilePath_RejectsAPathThatIsNotAnAbsoluteWindowsFilePath(string path)
    {
        Assert.False(PathRules.IsValidAbsoluteFilePath(path));
    }

    [Fact]
    public void ValidAbsoluteFilePath_RejectsAnInvalidPathWithItsCodeAndMessage()
    {
        ValidationFailure failure = Assert.Single(new SampleValidator().Validate(new Sample(@"..\a.json")).Errors);

        Assert.Equal("InvalidFilePath", failure.ErrorCode);
        Assert.Equal(@"Sample.Path Is Not A Valid Absolute Windows File Path (X:\...)", failure.ErrorMessage);
    }

    //A missing path is the error of RequiredWithMaxLength
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"D:\a.json")]
    public void ValidAbsoluteFilePath_AcceptsAMissingPathAndAValidPath(string? path)
    {
        Assert.True(new SampleValidator().Validate(new Sample(path)).IsValid);
    }

    private sealed record Sample(string? Path);

    private sealed class SampleValidator : AbstractValidator<Sample>
    {
        public SampleValidator()
        {
            RuleFor(x => x.Path!).ValidAbsoluteFilePath(_ => "Sample.Path");
        }
    }
}
