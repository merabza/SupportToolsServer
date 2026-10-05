using System;
using FluentValidation.Results;
using SupportToolsServer.Application.Settings;
using SupportToolsServer.Application.Settings.UpdateGlobalSettings;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.Settings;

public sealed class GlobalSettingsValidatorsTests
{
    private static ValidationResult Validate(StsGlobalSettingsDataModel model)
    {
        return new GlobalSettingsModelValidator().Validate(model);
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
        Assert.True(Validate(TestData.GlobalSettingsModel("Exchange", "Reduce", "Keep", "Bagetter", "Backups",
            "Reduce", "Keep", 3)).IsValid);
    }

    //The client model allows every value to be missing; it stores empty strings as well
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_AcceptsSettingsWithoutTheOptionalValues(string? value)
    {
        var model = new StsGlobalSettingsDataModel
        {
            ServiceDescriptionSignature = value,
            UploadTempExtension = value,
            ProgramArchiveDateMask = value,
            ProgramArchiveExtension = value,
            ParametersFileDateMask = value,
            ParametersFileExtension = value,
            MediatRLicenseKey = value,
            FileStorageNameForExchange = value,
            SmartSchemaNameForExchange = value,
            SmartSchemaNameForLocal = value,
            LocalPackageManagerWebApiClientName = value,
            DatabasesBackupFilesExchange = new StsDatabasesBackupFilesExchangeDataModel
            {
                DownloadTempExtension = value,
                UploadTempExtension = value,
                ExchangeFileStorageName = value,
                ExchangeSmartSchemaName = value,
                LocalSmartSchemaName = value
            }
        };

        Assert.True(Validate(model).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        StsGlobalSettingsDataModel model = TestData.GlobalSettingsModel(new string('f', 100), new string('s', 100),
            new string('l', 100), new string('a', 100), new string('e', 100), new string('x', 100),
            new string('o', 100));
        model.ServiceDescriptionSignature = new string('d', 100);
        model.UploadTempExtension = new string('u', 50);
        model.ProgramArchiveDateMask = new string('m', 50);
        model.ProgramArchiveExtension = new string('z', 50);
        model.ParametersFileDateMask = new string('p', 50);
        model.ParametersFileExtension = new string('j', 50);
        model.MediatRLicenseKey = new string('k', 4000);
        model.DatabasesBackupFilesExchange.DownloadTempExtension = new string('w', 50);
        model.DatabasesBackupFilesExchange.UploadTempExtension = new string('v', 50);

        Assert.True(Validate(model).IsValid);
    }

    [Theory]
    [InlineData(nameof(StsGlobalSettingsDataModel.ServiceDescriptionSignature), 100)]
    [InlineData(nameof(StsGlobalSettingsDataModel.UploadTempExtension), 50)]
    [InlineData(nameof(StsGlobalSettingsDataModel.ProgramArchiveDateMask), 50)]
    [InlineData(nameof(StsGlobalSettingsDataModel.ProgramArchiveExtension), 50)]
    [InlineData(nameof(StsGlobalSettingsDataModel.ParametersFileDateMask), 50)]
    [InlineData(nameof(StsGlobalSettingsDataModel.ParametersFileExtension), 50)]
    [InlineData(nameof(StsGlobalSettingsDataModel.MediatRLicenseKey), 4000)]
    [InlineData(nameof(StsGlobalSettingsDataModel.FileStorageNameForExchange), 100)]
    [InlineData(nameof(StsGlobalSettingsDataModel.SmartSchemaNameForExchange), 100)]
    [InlineData(nameof(StsGlobalSettingsDataModel.SmartSchemaNameForLocal), 100)]
    [InlineData(nameof(StsGlobalSettingsDataModel.LocalPackageManagerWebApiClientName), 100)]
    public void ModelValidator_RejectsALongerValue(string propertyName, int maxLength)
    {
        StsGlobalSettingsDataModel model = TestData.GlobalSettingsModel();
        typeof(StsGlobalSettingsDataModel).GetProperty(propertyName)!.SetValue(model,
            new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong", $"{propertyName} Is Longer Than {maxLength} Characters");
    }

    //The exchange parameters hold an UploadTempExtension of their own, so their messages name the part
    [Theory]
    [InlineData(nameof(StsDatabasesBackupFilesExchangeDataModel.DownloadTempExtension), 50)]
    [InlineData(nameof(StsDatabasesBackupFilesExchangeDataModel.UploadTempExtension), 50)]
    [InlineData(nameof(StsDatabasesBackupFilesExchangeDataModel.ExchangeFileStorageName), 100)]
    [InlineData(nameof(StsDatabasesBackupFilesExchangeDataModel.ExchangeSmartSchemaName), 100)]
    [InlineData(nameof(StsDatabasesBackupFilesExchangeDataModel.LocalSmartSchemaName), 100)]
    public void ModelValidator_RejectsALongerExchangeValueNamingThePart(string propertyName, int maxLength)
    {
        StsGlobalSettingsDataModel model = TestData.GlobalSettingsModel();
        typeof(StsDatabasesBackupFilesExchangeDataModel).GetProperty(propertyName)!.SetValue(
            model.DatabasesBackupFilesExchange, new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong",
            $"DatabasesBackupFilesExchange.{propertyName} Is Longer Than {maxLength} Characters");
    }

    //The client always sends the part, an empty one too; null means that the body held null
    [Fact]
    public void ModelValidator_RejectsMissingExchangeParameters()
    {
        StsGlobalSettingsDataModel model = TestData.GlobalSettingsModel();
        model.DatabasesBackupFilesExchange = null!;

        AssertSingleError(Validate(model), "ValueRequired", "DatabasesBackupFilesExchange Is Required");
    }

    //The license key is a secret: the message names the field, never the value
    [Fact]
    public void ModelValidator_DoesNotWriteTheLicenseKeyIntoItsMessage()
    {
        StsGlobalSettingsDataModel model = TestData.GlobalSettingsModel();
        model.MediatRLicenseKey = "made-up-license-key-" + new string('k', 4000);

        ValidationResult result = Validate(model);

        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.DoesNotContain("made-up", failure.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateGlobalSettingsCommandValidator();
        StsGlobalSettingsDataModel invalid = TestData.GlobalSettingsModel();
        invalid.UploadTempExtension = new string('u', 51);

        Assert.True(validator.Validate(new UpdateGlobalSettingsCommand(TestData.GlobalSettingsModel(version: 3)))
            .IsValid);
        AssertSingleError(validator.Validate(new UpdateGlobalSettingsCommand(invalid)), "ValueTooLong",
            "UploadTempExtension Is Longer Than 50 Characters");
    }
}
