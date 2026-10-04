using FluentValidation.Results;
using SupportToolsServer.Application.NpmPackages;
using SupportToolsServer.Application.NpmPackages.UpdateNpmPackage;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.NpmPackages;

public sealed class NpmPackageValidatorsTests
{
    private static ValidationResult Validate(StsNpmPackageDataModel model)
    {
        return new NpmPackageModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UI library")]
    public void ModelValidator_AcceptsAnNpmPackageWithOrWithoutDescription(string? description)
    {
        Assert.True(Validate(TestData.NpmPackageModel("react", description)).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        Assert.True(Validate(TestData.NpmPackageModel(new string('n', 214), new string('d', 255))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.NpmPackageModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.NpmPackageModel(new string('n', 215))), "ValueTooLong",
            "Name Is Longer Than 214 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerDescriptionNamingTheNpmPackage()
    {
        AssertSingleError(Validate(TestData.NpmPackageModel("react", new string('d', 256))), "ValueTooLong",
            "react.Description Is Longer Than 255 Characters");
    }

    //Without a name the message of the description cannot name the package
    [Fact]
    public void ModelValidator_NamesOnlyTheDescription_WhenTheNameIsMissing()
    {
        ValidationResult result = Validate(TestData.NpmPackageModel(" ", new string('d', 256)));

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "Description Is Longer Than 255 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateNpmPackageCommandValidator();

        Assert.True(validator.Validate(new UpdateNpmPackageCommand(TestData.NpmPackageModel("react", null, 3)))
            .IsValid);
        AssertSingleError(validator.Validate(new UpdateNpmPackageCommand(TestData.NpmPackageModel(""))),
            "ValueRequired", "Name Is Required");
    }
}
