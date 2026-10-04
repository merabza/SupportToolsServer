using FluentValidation.Results;
using SupportToolsServer.Application.Servers;
using SupportToolsServer.Application.Servers.UpdateServer;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.Servers;

public sealed class ServerValidatorsTests
{
    private static ValidationResult Validate(StsServerDataModel model)
    {
        return new ServerModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void ModelValidator_AcceptsAServerWithEveryValue()
    {
        Assert.True(Validate(TestData.ServerModel("dl360", "Dl360.WebAgent", "Dl360.Installer", "linux-x64"))
            .IsValid);
    }

    //The client model allows every value but the name to be missing; it stores empty strings as well
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_AcceptsAServerWithoutTheOptionalValues(string? value)
    {
        StsServerDataModel model = TestData.ServerModel("dl360", value, value, value);
        model.FilesUserName = value;
        model.FilesUsersGroupName = value;
        model.ServerSideDownloadFolder = value;
        model.ServerSideDeployFolder = value;

        Assert.True(Validate(model).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        StsServerDataModel model = TestData.ServerModel(new string('n', 100), new string('w', 100),
            new string('i', 100), new string('r', 50));
        model.FilesUserName = new string('u', 128);
        model.FilesUsersGroupName = new string('g', 128);
        model.ServerSideDownloadFolder = new string('d', 260);
        model.ServerSideDeployFolder = new string('p', 260);

        Assert.True(Validate(model).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.ServerModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.ServerModel(new string('n', 101))), "ValueTooLong",
            "Name Is Longer Than 100 Characters");
    }

    [Theory]
    [InlineData(nameof(StsServerDataModel.WebAgentName), 100)]
    [InlineData(nameof(StsServerDataModel.WebAgentInstallerName), 100)]
    [InlineData(nameof(StsServerDataModel.FilesUserName), 128)]
    [InlineData(nameof(StsServerDataModel.FilesUsersGroupName), 128)]
    [InlineData(nameof(StsServerDataModel.Runtime), 50)]
    [InlineData(nameof(StsServerDataModel.ServerSideDownloadFolder), 260)]
    [InlineData(nameof(StsServerDataModel.ServerSideDeployFolder), 260)]
    public void ModelValidator_RejectsALongerValueNamingTheServer(string propertyName, int maxLength)
    {
        StsServerDataModel model = TestData.ServerModel("dl360");
        typeof(StsServerDataModel).GetProperty(propertyName)!.SetValue(model, new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong",
            $"dl360.{propertyName} Is Longer Than {maxLength} Characters");
    }

    //Without a name the messages of the other values cannot name the server
    [Fact]
    public void ModelValidator_NamesOnlyTheValues_WhenTheNameIsMissing()
    {
        StsServerDataModel model = TestData.ServerModel(" ", runtime: new string('r', 51));

        ValidationResult result = Validate(model);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "Runtime Is Longer Than 50 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateServerCommandValidator();

        Assert.True(validator.Validate(new UpdateServerCommand(TestData.ServerModel("dl360", version: 3))).IsValid);
        AssertSingleError(validator.Validate(new UpdateServerCommand(TestData.ServerModel(""))), "ValueRequired",
            "Name Is Required");
    }
}
