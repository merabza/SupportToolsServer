using System;
using FluentValidation.Results;
using SupportToolsServer.Application.DatabaseServerConnections;
using SupportToolsServer.Application.DatabaseServerConnections.UpdateDatabaseServerConnection;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.DatabaseServerConnections;

public sealed class DatabaseServerConnectionValidatorsTests
{
    private static ValidationResult Validate(StsDatabaseServerConnectionDataModel model)
    {
        return new DatabaseServerConnectionModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void ModelValidator_AcceptsAConnectionWithEveryValue()
    {
        Assert.True(Validate(TestData.DatabaseServerConnectionModel("Pc1.Sql", "Pc1.WebAgent", ["Default", "Second"]))
            .IsValid);
    }

    //The client model allows every text but the provider to be missing; it stores empty strings as well
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_AcceptsAConnectionWithoutTheOptionalValues(string? value)
    {
        StsDatabaseServerConnectionDataModel model = TestData.DatabaseServerConnectionModel("Pc1.Sql", value);
        model.RemoteDbConnectionName = value;
        model.ServerAddress = value;
        model.ServerUser = value;
        model.ServerPass = value;
        model.DatabaseFoldersSets = [new StsDatabaseFoldersSetDataModel { Name = "Default" }];

        Assert.True(Validate(model).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        StsDatabaseServerConnectionDataModel model =
            TestData.DatabaseServerConnectionModel(new string('n', 100), new string('w', 100));
        model.DatabaseServerProvider = new string('p', 50);
        model.RemoteDbConnectionName = new string('r', 100);
        model.ServerAddress = new string('a', 256);
        model.ServerUser = new string('u', 128);
        model.ServerPass = new string('s', 256);
        model.DatabaseFoldersSets =
        [
            new StsDatabaseFoldersSetDataModel
            {
                Name = new string('f', 50),
                Backup = new string('b', 260),
                Data = new string('d', 260),
                DataLog = new string('l', 260)
            }
        ];

        Assert.True(Validate(model).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.DatabaseServerConnectionModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.DatabaseServerConnectionModel(new string('n', 101))), "ValueTooLong",
            "Name Is Longer Than 100 Characters");
    }

    //The client always sends the name of its enum; JSON can still send null for it
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingProviderNamingTheConnection(string? provider)
    {
        StsDatabaseServerConnectionDataModel model = TestData.DatabaseServerConnectionModel("Pc1.Sql");
        model.DatabaseServerProvider = provider!;

        AssertSingleError(Validate(model), "ValueRequired", "Pc1.Sql.DatabaseServerProvider Is Required");
    }

    [Theory]
    [InlineData(nameof(StsDatabaseServerConnectionDataModel.DatabaseServerProvider), 50)]
    [InlineData(nameof(StsDatabaseServerConnectionDataModel.DbWebAgentName), 100)]
    [InlineData(nameof(StsDatabaseServerConnectionDataModel.RemoteDbConnectionName), 100)]
    [InlineData(nameof(StsDatabaseServerConnectionDataModel.ServerAddress), 256)]
    [InlineData(nameof(StsDatabaseServerConnectionDataModel.ServerUser), 128)]
    [InlineData(nameof(StsDatabaseServerConnectionDataModel.ServerPass), 256)]
    public void ModelValidator_RejectsALongerValueNamingTheConnection(string propertyName, int maxLength)
    {
        StsDatabaseServerConnectionDataModel model = TestData.DatabaseServerConnectionModel("Pc1.Sql");
        typeof(StsDatabaseServerConnectionDataModel).GetProperty(propertyName)!.SetValue(model,
            new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong",
            $"Pc1.Sql.{propertyName} Is Longer Than {maxLength} Characters");
    }

    //The message names the value, never the secret itself
    [Fact]
    public void ModelValidator_RejectsALongerPasswordWithoutShowingIt()
    {
        StsDatabaseServerConnectionDataModel model = TestData.DatabaseServerConnectionModel("Pc1.Sql");
        model.ServerPass = TestData.MadeUpPassword + new string('s', 256);

        ValidationResult result = Validate(model);

        AssertSingleError(result, "ValueTooLong", "Pc1.Sql.ServerPass Is Longer Than 256 Characters");
        Assert.DoesNotContain(TestData.MadeUpPassword, result.Errors[0].ErrorMessage, StringComparison.Ordinal);
    }

    //JSON can still send null for the list; the client sends its missing dictionary as an empty list
    [Fact]
    public void ModelValidator_RejectsMissingFoldersSetsNamingTheConnection()
    {
        StsDatabaseServerConnectionDataModel model = TestData.DatabaseServerConnectionModel("Pc1.Sql");
        model.DatabaseFoldersSets = null!;

        AssertSingleError(Validate(model), "ValueRequired", "Pc1.Sql.DatabaseFoldersSets Is Required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAFoldersSetWithoutName(string? name)
    {
        AssertSingleError(Validate(TestData.DatabaseServerConnectionModel("Pc1.Sql", null, [name!])),
            "ValueRequired", "DatabaseFoldersSets.Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerFoldersSetName()
    {
        AssertSingleError(Validate(TestData.DatabaseServerConnectionModel("Pc1.Sql", null, [new string('f', 51)])),
            "ValueTooLong", "DatabaseFoldersSets.Name Is Longer Than 50 Characters");
    }

    [Theory]
    [InlineData(nameof(StsDatabaseFoldersSetDataModel.Backup))]
    [InlineData(nameof(StsDatabaseFoldersSetDataModel.Data))]
    [InlineData(nameof(StsDatabaseFoldersSetDataModel.DataLog))]
    public void ModelValidator_RejectsALongerFolderNamingTheFoldersSet(string propertyName)
    {
        StsDatabaseServerConnectionDataModel model =
            TestData.DatabaseServerConnectionModel("Pc1.Sql", null, ["Default"]);
        typeof(StsDatabaseFoldersSetDataModel).GetProperty(propertyName)!.SetValue(model.DatabaseFoldersSets[0],
            new string('x', 261));

        AssertSingleError(Validate(model), "ValueTooLong",
            $"DatabaseFoldersSets.Default.{propertyName} Is Longer Than 260 Characters");
    }

    [Fact]
    public void ModelValidator_NamesTheFolderOnly_WhenTheFoldersSetHasNoName()
    {
        StsDatabaseServerConnectionDataModel model = TestData.DatabaseServerConnectionModel("Pc1.Sql", null, [""]);
        model.DatabaseFoldersSets[0].Backup = new string('x', 261);

        ValidationResult result = Validate(model);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "DatabaseFoldersSets.Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" &&
                 e.ErrorMessage == "DatabaseFoldersSets.Backup Is Longer Than 260 Characters");
    }

    //The name of a folders set is the key of the client's dictionary, and the names match without case
    [Theory]
    [InlineData("Default", "Default")]
    [InlineData("Default", "DEFAULT")]
    public void ModelValidator_RejectsARepeatedFoldersSetNameNamingTheConnection(string first, string second)
    {
        AssertSingleError(
            Validate(TestData.DatabaseServerConnectionModel("Pc1.Sql", null, [first, "Second", second])),
            "ValuesNotUnique", "Pc1.Sql.DatabaseFoldersSets.Name Values Are Not Unique");
    }

    //Without a name the messages of the other values cannot name the connection
    [Fact]
    public void ModelValidator_NamesOnlyTheValues_WhenTheNameIsMissing()
    {
        StsDatabaseServerConnectionDataModel model = TestData.DatabaseServerConnectionModel(" ");
        model.DatabaseServerProvider = "";
        model.ServerPass = new string('s', 257);

        ValidationResult result = Validate(model);

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "DatabaseServerProvider Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "ServerPass Is Longer Than 256 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateDatabaseServerConnectionCommandValidator();

        Assert.True(validator
            .Validate(new UpdateDatabaseServerConnectionCommand(
                TestData.DatabaseServerConnectionModel("Pc1.Sql", version: 3))).IsValid);
        AssertSingleError(
            validator.Validate(new UpdateDatabaseServerConnectionCommand(
                TestData.DatabaseServerConnectionModel("Pc1.Sql", null, ["Default", "default"]))), "ValuesNotUnique",
            "Pc1.Sql.DatabaseFoldersSets.Name Values Are Not Unique");
    }
}
