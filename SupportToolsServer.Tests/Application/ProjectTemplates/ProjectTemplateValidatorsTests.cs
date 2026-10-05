using FluentValidation.Results;
using SupportToolsServer.Application.ProjectTemplates;
using SupportToolsServer.Application.ProjectTemplates.UpdateProjectTemplate;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.ProjectTemplates;

public sealed class ProjectTemplateValidatorsTests
{
    private static ValidationResult Validate(StsProjectTemplateDataModel model)
    {
        return new ProjectTemplateModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    //The names of the client hold spaces, such as "Console With Database and Menu"
    [Fact]
    public void ModelValidator_AcceptsATemplateWithEveryValue()
    {
        Assert.True(Validate(TestData.ProjectTemplateModel("Console With Database and Menu", "redux-typescript", 3))
            .IsValid);
    }

    //The client model allows the test project names and the React template to be missing
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_AcceptsATemplateWithoutTheOptionalValues(string? value)
    {
        StsProjectTemplateDataModel model = TestData.ProjectTemplateModel("Console", value);
        model.TestProjectName = value;
        model.TestProjectShortName = value;

        Assert.True(Validate(model).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        StsProjectTemplateDataModel model =
            TestData.ProjectTemplateModel(new string('n', 100), new string('r', 50));
        model.SupportProjectType = new string('s', 50);
        model.TestProjectName = new string('t', 100);
        model.TestProjectShortName = new string('h', 100);

        Assert.True(Validate(model).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.ProjectTemplateModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.ProjectTemplateModel(new string('n', 101))), "ValueTooLong",
            "Name Is Longer Than 100 Characters");
    }

    //The server does not know the ESupportProjectType of the client: the type is required and checked for its length
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingSupportProjectTypeNamingTheTemplate(string supportProjectType)
    {
        StsProjectTemplateDataModel model = TestData.ProjectTemplateModel("Console");
        model.SupportProjectType = supportProjectType;

        AssertSingleError(Validate(model), "ValueRequired", "Console.SupportProjectType Is Required");
    }

    [Theory]
    [InlineData(nameof(StsProjectTemplateDataModel.SupportProjectType), 50)]
    [InlineData(nameof(StsProjectTemplateDataModel.TestProjectName), 100)]
    [InlineData(nameof(StsProjectTemplateDataModel.TestProjectShortName), 100)]
    [InlineData(nameof(StsProjectTemplateDataModel.ReactTemplateName), 50)]
    public void ModelValidator_RejectsALongerValueNamingTheTemplate(string propertyName, int maxLength)
    {
        StsProjectTemplateDataModel model = TestData.ProjectTemplateModel("Console");
        typeof(StsProjectTemplateDataModel).GetProperty(propertyName)!.SetValue(model,
            new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong",
            $"Console.{propertyName} Is Longer Than {maxLength} Characters");
    }

    //Without a name the messages of the other values cannot name the template
    [Fact]
    public void ModelValidator_NamesOnlyTheValues_WhenTheNameIsMissing()
    {
        StsProjectTemplateDataModel model = TestData.ProjectTemplateModel(" ", new string('r', 51));

        ValidationResult result = Validate(model);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "ReactTemplateName Is Longer Than 50 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateProjectTemplateCommandValidator();

        Assert.True(validator.Validate(new UpdateProjectTemplateCommand(TestData.ProjectTemplateModel("Console")))
            .IsValid);
        AssertSingleError(validator.Validate(new UpdateProjectTemplateCommand(TestData.ProjectTemplateModel(""))),
            "ValueRequired", "Name Is Required");
    }
}
