using FluentValidation.Results;
using SupportToolsServer.Application.SmartSchemas;
using SupportToolsServer.Application.SmartSchemas.UpdateSmartSchema;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.SmartSchemas;

public sealed class SmartSchemaValidatorsTests
{
    private static ValidationResult Validate(StsSmartSchemaDataModel model)
    {
        return new SmartSchemaModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void ModelValidator_AcceptsASmartSchemaWithDetails()
    {
        Assert.True(Validate(TestData.SmartSchemaModel("Reduce", 1, [("Year", 1), ("Month", 1), ("Day", 2)]))
            .IsValid);
    }

    //A schema without details keeps only the last files
    [Fact]
    public void ModelValidator_AcceptsASmartSchemaWithoutDetails()
    {
        Assert.True(Validate(TestData.SmartSchemaModel("Reduce")).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        Assert.True(Validate(TestData.SmartSchemaModel(new string('n', 100), 1, [(new string('p', 50), 1)])).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.SmartSchemaModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.SmartSchemaModel(new string('n', 101))), "ValueTooLong",
            "Name Is Longer Than 100 Characters");
    }

    //JSON can still send null for the list
    [Fact]
    public void ModelValidator_RejectsMissingDetailsNamingTheSmartSchema()
    {
        StsSmartSchemaDataModel model = TestData.SmartSchemaModel("Reduce");
        model.Details = null!;

        AssertSingleError(Validate(model), "ValueRequired", "Reduce.Details Is Required");
    }

    //The server does not know the enum of the client, so any period type of a valid length passes
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsADetailWithoutPeriodType(string? periodType)
    {
        AssertSingleError(Validate(TestData.SmartSchemaModel("Reduce", 1, [(periodType!, 1)])), "ValueRequired",
            "Details.PeriodType Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerPeriodType()
    {
        AssertSingleError(Validate(TestData.SmartSchemaModel("Reduce", 1, [(new string('p', 51), 1)])),
            "ValueTooLong", "Details.PeriodType Is Longer Than 50 Characters");
    }

    //The client looks a detail up by its period type, and the names match without case
    [Theory]
    [InlineData("Day", "Day")]
    [InlineData("Day", "DAY")]
    public void ModelValidator_RejectsARepeatedPeriodTypeNamingTheSmartSchema(string first, string second)
    {
        AssertSingleError(Validate(TestData.SmartSchemaModel("Reduce", 1, [(first, 1), ("Week", 1), (second, 2)])),
            "ValuesNotUnique", "Reduce.Details.PeriodType Values Are Not Unique");
    }

    //Without a name the messages of the other values cannot name the schema
    [Fact]
    public void ModelValidator_NamesOnlyTheValues_WhenTheNameIsMissing()
    {
        ValidationResult result = Validate(TestData.SmartSchemaModel(" ", 1, [("Day", 1), ("Day", 2)]));

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValuesNotUnique" && e.ErrorMessage == "Details.PeriodType Values Are Not Unique");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateSmartSchemaCommandValidator();

        Assert.True(validator
            .Validate(new UpdateSmartSchemaCommand(TestData.SmartSchemaModel("Reduce", 1, [("Day", 1)], 3))).IsValid);
        AssertSingleError(
            validator.Validate(new UpdateSmartSchemaCommand(TestData.SmartSchemaModel("Reduce", 1, [("", 1)]))),
            "ValueRequired", "Details.PeriodType Is Required");
    }
}
