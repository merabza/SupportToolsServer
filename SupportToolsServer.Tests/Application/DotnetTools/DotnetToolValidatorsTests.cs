using FluentValidation.Results;
using SupportToolsServer.Application.DotnetTools;
using SupportToolsServer.Application.DotnetTools.UpdateDotnetTool;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.DotnetTools;

public sealed class DotnetToolValidatorsTests
{
    private static ValidationResult Validate(StsDotnetToolDataModel model)
    {
        return new DotnetToolModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void ModelValidator_AcceptsADotnetToolWithEveryValue()
    {
        Assert.True(Validate(TestData.DotnetToolModel("DotnetEf", "dotnet-ef", "9.0.8", "Entity Framework")).IsValid);
    }

    //Without a maximum version the latest one is installed; the client stores an empty string as well
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_AcceptsADotnetToolWithoutMaxVersionAndDescription(string? value)
    {
        Assert.True(Validate(TestData.DotnetToolModel("DotnetEf", "dotnet-ef", value, value)).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        Assert.True(Validate(TestData.DotnetToolModel(new string('n', 100), new string('p', 100), new string('v', 64),
            new string('d', 255))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.DotnetToolModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.DotnetToolModel(new string('n', 101))), "ValueTooLong",
            "Name Is Longer Than 100 Characters");
    }

    //A tool without a package can be neither checked nor installed; JSON can still send null for it
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingPackageIdNamingTheDotnetTool(string? packageId)
    {
        AssertSingleError(Validate(TestData.DotnetToolModel("DotnetEf", packageId!)), "ValueRequired",
            "DotnetEf.PackageId Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerPackageIdNamingTheDotnetTool()
    {
        AssertSingleError(Validate(TestData.DotnetToolModel("DotnetEf", new string('p', 101))), "ValueTooLong",
            "DotnetEf.PackageId Is Longer Than 100 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerMaxVersionNamingTheDotnetTool()
    {
        AssertSingleError(Validate(TestData.DotnetToolModel("DotnetEf", "dotnet-ef", new string('v', 65))),
            "ValueTooLong", "DotnetEf.MaxVersion Is Longer Than 64 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerDescriptionNamingTheDotnetTool()
    {
        AssertSingleError(Validate(TestData.DotnetToolModel("DotnetEf", "dotnet-ef", null, new string('d', 256))),
            "ValueTooLong", "DotnetEf.Description Is Longer Than 255 Characters");
    }

    //Without a name the messages of the other values cannot name the tool
    [Fact]
    public void ModelValidator_NamesOnlyTheValues_WhenTheNameIsMissing()
    {
        ValidationResult result = Validate(TestData.DotnetToolModel(" ", "", new string('v', 65),
            new string('d', 256)));

        Assert.Equal(4, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "PackageId Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "MaxVersion Is Longer Than 64 Characters");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "Description Is Longer Than 255 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateDotnetToolCommandValidator();

        Assert.True(validator.Validate(new UpdateDotnetToolCommand(TestData.DotnetToolModel("DotnetEf", version: 3)))
            .IsValid);
        AssertSingleError(validator.Validate(new UpdateDotnetToolCommand(TestData.DotnetToolModel("DotnetEf", ""))),
            "ValueRequired", "DotnetEf.PackageId Is Required");
    }
}
