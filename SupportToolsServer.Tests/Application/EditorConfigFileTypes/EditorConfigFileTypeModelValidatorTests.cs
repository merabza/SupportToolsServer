using FluentValidation.Results;
using SupportToolsServer.Application.EditorConfigFileTypes;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.EditorConfigFileTypes;

public sealed class EditorConfigFileTypeModelValidatorTests
{
    private static ValidationResult Validate(StsEditorConfigFileTypeDataModel model)
    {
        return new EditorConfigFileTypeModelValidator().Validate(model);
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
        Assert.True(Validate(TestData.EditorConfigModel("default")).IsValid);
    }

    [Fact]
    public void Validate_AcceptsAnEmptyContent()
    {
        Assert.True(Validate(TestData.EditorConfigModel("default", string.Empty)).IsValid);
    }

    //.editorconfig files are much longer than .gitignore files, so the limit is larger than the gitignore one
    [Fact]
    public void Validate_AcceptsValuesOfTheMaximumLengths()
    {
        Assert.True(Validate(TestData.EditorConfigModel(new string('n', 50), new string('c', 65536))).IsValid);
    }

    [Fact]
    public void Validate_RejectsAMissingName()
    {
        AssertSingleError(Validate(TestData.EditorConfigModel(" ")), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void Validate_RejectsATooLongName()
    {
        AssertSingleError(Validate(TestData.EditorConfigModel(new string('n', 51))), "ValueTooLong",
            "Name Is Longer Than 50 Characters");
    }

    [Fact]
    public void Validate_RejectsAMissingContent_NamingTheType()
    {
        StsEditorConfigFileTypeDataModel model = TestData.EditorConfigModel("default");
        model.Content = null!;

        AssertSingleError(Validate(model), "ValueRequired", "default.Content Is Required");
    }

    [Fact]
    public void Validate_RejectsATooLongContent_NamingTheType()
    {
        AssertSingleError(Validate(TestData.EditorConfigModel("default", new string('c', 65537))), "ValueTooLong",
            "default.Content Is Longer Than 65536 Characters");
    }

    [Fact]
    public void Validate_NamesOnlyTheContent_WhenTheTypeHasNoName()
    {
        StsEditorConfigFileTypeDataModel model = TestData.EditorConfigModel(string.Empty);
        model.Content = null!;

        ValidationResult result = Validate(model);

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Content Is Required");
    }
}
