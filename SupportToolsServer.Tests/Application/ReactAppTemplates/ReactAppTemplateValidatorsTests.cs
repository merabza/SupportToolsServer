using FluentValidation.Results;
using SupportToolsServer.Application.ReactAppTemplates;
using SupportToolsServer.Application.ReactAppTemplates.UpdateReactAppTemplate;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.ReactAppTemplates;

public sealed class ReactAppTemplateValidatorsTests
{
    private static ValidationResult Validate(StsReactAppTemplateDataModel model)
    {
        return new ReactAppTemplateModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void ModelValidator_AcceptsAReactAppTemplateWithATemplate()
    {
        Assert.True(Validate(TestData.ReactAppTemplateModel("ReduxApp", "redux-typescript")).IsValid);
    }

    //A template without the --template value is meaningless; JSON can still send null for it
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingTemplateNamingTheReactAppTemplate(string? template)
    {
        var model = new StsReactAppTemplateDataModel { Name = "ReduxApp", Template = template! };

        AssertSingleError(Validate(model), "ValueRequired", "ReduxApp.Template Is Required");
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        Assert.True(Validate(TestData.ReactAppTemplateModel(new string('n', 50), new string('t', 214))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.ReactAppTemplateModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.ReactAppTemplateModel(new string('n', 51))), "ValueTooLong",
            "Name Is Longer Than 50 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerTemplateNamingTheReactAppTemplate()
    {
        AssertSingleError(Validate(TestData.ReactAppTemplateModel("ReduxApp", new string('t', 215))), "ValueTooLong",
            "ReduxApp.Template Is Longer Than 214 Characters");
    }

    //Without a name the message of the template value cannot name the React app template
    [Fact]
    public void ModelValidator_NamesOnlyTheTemplate_WhenTheNameIsMissing()
    {
        ValidationResult result = Validate(TestData.ReactAppTemplateModel(" ", new string('t', 215)));

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "Template Is Longer Than 214 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateReactAppTemplateCommandValidator();

        Assert.True(validator
            .Validate(new UpdateReactAppTemplateCommand(TestData.ReactAppTemplateModel("ReduxApp", "typescript", 3)))
            .IsValid);
        AssertSingleError(validator.Validate(new UpdateReactAppTemplateCommand(TestData.ReactAppTemplateModel(""))),
            "ValueRequired", "Name Is Required");
    }
}
