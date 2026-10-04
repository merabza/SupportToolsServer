using System;
using FluentValidation.Results;
using SupportToolsServer.Application.FileStorages;
using SupportToolsServer.Application.FileStorages.UpdateFileStorage;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.FileStorages;

public sealed class FileStorageValidatorsTests
{
    private static ValidationResult Validate(StsFileStorageDataModel model)
    {
        return new FileStorageModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void ModelValidator_AcceptsAFileStorageWithEveryValue()
    {
        Assert.True(Validate(TestData.FileStorageModel("Exchange")).IsValid);
    }

    //A local folder needs neither a user nor a password; the client stores empty strings as well
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_AcceptsAFileStorageWithoutPathUserAndPassword(string? value)
    {
        Assert.True(Validate(TestData.FileStorageModel("Exchange", value, value, value)).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        Assert.True(Validate(TestData.FileStorageModel(new string('n', 100), new string('p', 260),
            new string('u', 128), new string('s', 256))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.FileStorageModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.FileStorageModel(new string('n', 101))), "ValueTooLong",
            "Name Is Longer Than 100 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerPathNamingTheFileStorage()
    {
        AssertSingleError(Validate(TestData.FileStorageModel("Exchange", new string('p', 261))), "ValueTooLong",
            "Exchange.FileStoragePath Is Longer Than 260 Characters");
    }

    //The message names the value, never the secret itself
    [Fact]
    public void ModelValidator_RejectsALongerUserNameNamingTheFileStorageButNotTheUser()
    {
        string userName = TestData.MadeUpUser + new string('u', 128);

        ValidationResult result = Validate(TestData.FileStorageModel("Exchange", userName: userName));

        AssertSingleError(result, "ValueTooLong", "Exchange.UserName Is Longer Than 128 Characters");
        Assert.DoesNotContain(TestData.MadeUpUser, result.Errors[0].ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ModelValidator_RejectsALongerPasswordNamingTheFileStorageButNotThePassword()
    {
        string password = TestData.MadeUpPassword + new string('s', 256);

        ValidationResult result = Validate(TestData.FileStorageModel("Exchange", password: password));

        AssertSingleError(result, "ValueTooLong", "Exchange.Password Is Longer Than 256 Characters");
        Assert.DoesNotContain(TestData.MadeUpPassword, result.Errors[0].ErrorMessage,
            StringComparison.Ordinal);
    }

    //Without a name the messages of the other values cannot name the file storage
    [Fact]
    public void ModelValidator_NamesOnlyTheValues_WhenTheNameIsMissing()
    {
        ValidationResult result = Validate(TestData.FileStorageModel(" ", new string('p', 261), new string('u', 129),
            new string('s', 257)));

        Assert.Equal(4, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "FileStoragePath Is Longer Than 260 Characters");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "UserName Is Longer Than 128 Characters");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "Password Is Longer Than 256 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateFileStorageCommandValidator();

        Assert.True(validator.Validate(new UpdateFileStorageCommand(TestData.FileStorageModel("Exchange", version: 3)))
            .IsValid);
        AssertSingleError(
            validator.Validate(new UpdateFileStorageCommand(TestData.FileStorageModel("Exchange",
                new string('p', 261)))), "ValueTooLong", "Exchange.FileStoragePath Is Longer Than 260 Characters");
    }
}
