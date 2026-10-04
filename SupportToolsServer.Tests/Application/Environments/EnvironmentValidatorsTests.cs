using FluentValidation.Results;
using SupportToolsServer.Application.Environments;
using SupportToolsServer.Application.Environments.UpdateEnvironment;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.Environments;

public sealed class EnvironmentValidatorsTests
{
    private static ValidationResult Validate(StsEnvironmentDataModel model)
    {
        return new EnvironmentModelValidator().Validate(model);
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
    [InlineData("Production")]
    public void ModelValidator_AcceptsAnEnvironmentWithOrWithoutDescription(string? description)
    {
        Assert.True(Validate(TestData.EnvironmentModel("Prod", description)).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        Assert.True(Validate(TestData.EnvironmentModel(new string('n', 50), new string('d', 255))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.EnvironmentModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.EnvironmentModel(new string('n', 51))), "ValueTooLong",
            "Name Is Longer Than 50 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerDescriptionNamingTheEnvironment()
    {
        AssertSingleError(Validate(TestData.EnvironmentModel("Prod", new string('d', 256))), "ValueTooLong",
            "Prod.Description Is Longer Than 255 Characters");
    }

    //Without a name the message of the description cannot name the environment
    [Fact]
    public void ModelValidator_NamesOnlyTheDescription_WhenTheNameIsMissing()
    {
        ValidationResult result = Validate(TestData.EnvironmentModel(" ", new string('d', 256)));

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "Description Is Longer Than 255 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateEnvironmentCommandValidator();

        Assert.True(
            validator.Validate(new UpdateEnvironmentCommand(TestData.EnvironmentModel("Prod", null, 3))).IsValid);
        AssertSingleError(validator.Validate(new UpdateEnvironmentCommand(TestData.EnvironmentModel(""))),
            "ValueRequired", "Name Is Required");
    }
}
