using FluentValidation.Results;
using SupportToolsServer.Application.Settings;
using SupportToolsServer.Application.Settings.UpdateProjectCreatorSettings;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.Settings;

public sealed class ProjectCreatorSettingsValidatorsTests
{
    private static ValidationResult Validate(StsProjectCreatorSettingsDataModel model)
    {
        return new ProjectCreatorSettingsModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void ModelValidator_AcceptsSettingsWithEveryValue()
    {
        Assert.True(Validate(TestData.ProjectCreatorSettingsModel("dl360", "Prod", "Pazisi", "Backups", "Reduce", 3))
            .IsValid);
    }

    //The client model allows every value to be missing; it stores empty strings as well. IndentSize has no rule, the
    //client has none either
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_AcceptsSettingsWithoutTheOptionalValues(string? value)
    {
        StsProjectCreatorSettingsDataModel model =
            TestData.ProjectCreatorSettingsModel(value, value, value, value, value);
        model.IndentSize = -1;
        model.FakeHostProjectName = value;
        model.ProjectsFolderPathReal = value;
        model.SecretsFolderPathReal = value;

        Assert.True(Validate(model).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        StsProjectCreatorSettingsDataModel model = TestData.ProjectCreatorSettingsModel(new string('s', 100),
            new string('e', 50), new string('c', 100), new string('f', 100), new string('m', 100));
        model.FakeHostProjectName = new string('h', 100);
        model.ProjectsFolderPathReal = new string('p', 260);
        model.SecretsFolderPathReal = new string('x', 260);

        Assert.True(Validate(model).IsValid);
    }

    [Theory]
    [InlineData(nameof(StsProjectCreatorSettingsDataModel.FakeHostProjectName), 100)]
    [InlineData(nameof(StsProjectCreatorSettingsDataModel.ProjectsFolderPathReal), 260)]
    [InlineData(nameof(StsProjectCreatorSettingsDataModel.SecretsFolderPathReal), 260)]
    [InlineData(nameof(StsProjectCreatorSettingsDataModel.ProductionServerName), 100)]
    [InlineData(nameof(StsProjectCreatorSettingsDataModel.ProductionEnvironmentName), 50)]
    [InlineData(nameof(StsProjectCreatorSettingsDataModel.DeveloperDbConnectionName), 100)]
    [InlineData(nameof(StsProjectCreatorSettingsDataModel.DatabaseExchangeFileStorageName), 100)]
    [InlineData(nameof(StsProjectCreatorSettingsDataModel.UseSmartSchema), 100)]
    public void ModelValidator_RejectsALongerValue(string propertyName, int maxLength)
    {
        StsProjectCreatorSettingsDataModel model = TestData.ProjectCreatorSettingsModel();
        typeof(StsProjectCreatorSettingsDataModel).GetProperty(propertyName)!.SetValue(model,
            new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong", $"{propertyName} Is Longer Than {maxLength} Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateProjectCreatorSettingsCommandValidator();

        Assert.True(validator
            .Validate(new UpdateProjectCreatorSettingsCommand(TestData.ProjectCreatorSettingsModel(version: 3)))
            .IsValid);
        AssertSingleError(
            validator.Validate(
                new UpdateProjectCreatorSettingsCommand(TestData.ProjectCreatorSettingsModel(new string('s', 101)))),
            "ValueTooLong", "ProductionServerName Is Longer Than 100 Characters");
    }
}
