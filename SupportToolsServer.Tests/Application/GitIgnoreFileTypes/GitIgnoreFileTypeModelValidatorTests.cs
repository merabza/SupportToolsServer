using FluentValidation.Results;
using SupportToolsServer.Application.GitIgnoreFileTypes;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitIgnoreFileTypes;

public sealed class GitIgnoreFileTypeModelValidatorTests
{
    private static ValidationResult Validate(StsGitIgnoreFileTypeDataModel model)
    {
        return new GitIgnoreFileTypeModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void Validate_AcceptsAFilledModel()
    {
        Assert.True(Validate(TestData.GitIgnoreModel("CSharp")).IsValid);
    }

    [Fact]
    public void Validate_AcceptsAnEmptyContent()
    {
        Assert.True(Validate(TestData.GitIgnoreModel("CSharp", string.Empty)).IsValid);
    }

    [Fact]
    public void Validate_AcceptsValuesOfTheMaximumLengths()
    {
        Assert.True(Validate(TestData.GitIgnoreModel(new string('n', 50), new string('c', 16384))).IsValid);
    }

    [Fact]
    public void Validate_RejectsAMissingName()
    {
        AssertSingleError(Validate(TestData.GitIgnoreModel(" ")), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void Validate_RejectsATooLongName()
    {
        AssertSingleError(Validate(TestData.GitIgnoreModel(new string('n', 51))), "ValueTooLong",
            "Name Is Longer Than 50 Characters");
    }

    [Fact]
    public void Validate_RejectsAMissingContent_NamingTheType()
    {
        StsGitIgnoreFileTypeDataModel model = TestData.GitIgnoreModel("CSharp");
        model.Content = null!;

        AssertSingleError(Validate(model), "ValueRequired", "CSharp.Content Is Required");
    }

    [Fact]
    public void Validate_RejectsATooLongContent_NamingTheType()
    {
        AssertSingleError(Validate(TestData.GitIgnoreModel("CSharp", new string('c', 16385))), "ValueTooLong",
            "CSharp.Content Is Longer Than 16384 Characters");
    }

    [Fact]
    public void Validate_NamesOnlyTheContent_WhenTheTypeHasNoName()
    {
        StsGitIgnoreFileTypeDataModel model = TestData.GitIgnoreModel(string.Empty);
        model.Content = null!;

        ValidationResult result = Validate(model);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Content Is Required");
    }
}
