using FluentValidation;
using FluentValidation.Results;
using SupportToolsServer.Application.Validation;
using Xunit;

namespace SupportToolsServer.Tests.Application.Validation;

public sealed class TextRulesTests
{
    private const int MaxLength = 5;

    private static ValidationResult Validate(string? value)
    {
        return new SampleValidator().Validate(new Sample("Owner", value));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("abcde")]
    public void RequiredWithMaxLength_AcceptsAFilledValueUpToTheMaximum(string value)
    {
        Assert.True(Validate(value).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequiredWithMaxLength_RejectsAMissingValue(string? value)
    {
        ValidationFailure failure = Assert.Single(Validate(value).Errors);

        Assert.Equal("ValueRequired", failure.ErrorCode);
        Assert.Equal("Owner.Value Is Required", failure.ErrorMessage);
    }

    [Fact]
    public void RequiredWithMaxLength_RejectsALongerValue()
    {
        ValidationFailure failure = Assert.Single(Validate("abcdef").Errors);

        Assert.Equal("ValueTooLong", failure.ErrorCode);
        Assert.Equal("Owner.Value Is Longer Than 5 Characters", failure.ErrorMessage);
    }

    private sealed record Sample(string Owner, string? Value);

    private sealed class SampleValidator : AbstractValidator<Sample>
    {
        public SampleValidator()
        {
            RuleFor(x => x.Value!).RequiredWithMaxLength(x => $"{x.Owner}.Value", MaxLength);
        }
    }
}
