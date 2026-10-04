using FluentValidation.Results;
using SupportToolsServer.Application.Runtimes;
using SupportToolsServer.Application.Runtimes.UpdateRuntime;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.Runtimes;

public sealed class RuntimeValidatorsTests
{
    private static ValidationResult Validate(StsRuntimeDataModel model)
    {
        return new RuntimeModelValidator().Validate(model);
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
    [InlineData("Windows x64")]
    public void ModelValidator_AcceptsARuntimeWithOrWithoutDescription(string? description)
    {
        Assert.True(Validate(TestData.RuntimeModel("win-x64", description)).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        Assert.True(Validate(TestData.RuntimeModel(new string('n', 50), new string('d', 255))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.RuntimeModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.RuntimeModel(new string('n', 51))), "ValueTooLong",
            "Name Is Longer Than 50 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerDescriptionNamingTheRuntime()
    {
        AssertSingleError(Validate(TestData.RuntimeModel("win-x64", new string('d', 256))), "ValueTooLong",
            "win-x64.Description Is Longer Than 255 Characters");
    }

    //Without a name the message of the description cannot name the runtime
    [Fact]
    public void ModelValidator_NamesOnlyTheDescription_WhenTheNameIsMissing()
    {
        ValidationResult result = Validate(TestData.RuntimeModel(" ", new string('d', 256)));

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "Description Is Longer Than 255 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateRuntimeCommandValidator();

        Assert.True(validator.Validate(new UpdateRuntimeCommand(TestData.RuntimeModel("win-x64", null, 3))).IsValid);
        AssertSingleError(validator.Validate(new UpdateRuntimeCommand(TestData.RuntimeModel(""))), "ValueRequired",
            "Name Is Required");
    }
}
