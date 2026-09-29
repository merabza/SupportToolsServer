using System.Collections.Generic;
using FluentValidation.Results;
using SupportToolsServer.Application.GitIgnoreFileTypes.EnsureGitIgnoreFileType;
using SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.GitIgnoreFileTypes;

public sealed class GitIgnoreFileTypeCommandValidatorsTests
{
    private static ValidationResult ValidateEnsure(string name)
    {
        return new EnsureGitIgnoreFileTypeCommandValidator().Validate(new EnsureGitIgnoreFileTypeCommand(name));
    }

    private static ValidationResult ValidateSyncUp(List<StsGitIgnoreFileTypeDataModel>? list)
    {
        return new SyncUpGitIgnoreFileTypesCommandValidator().Validate(new SyncUpGitIgnoreFileTypesCommand(false,
            list!));
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Theory]
    [InlineData("CSharp")]
    [InlineData("12345678901234567890123456789012345678901234567890")]
    public void EnsureValidator_AcceptsANameUpToTheMaximumLength(string name)
    {
        Assert.True(ValidateEnsure(name).IsValid);
    }

    [Fact]
    public void EnsureValidator_RejectsAMissingName()
    {
        AssertSingleError(ValidateEnsure(" "), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void EnsureValidator_RejectsATooLongName()
    {
        AssertSingleError(ValidateEnsure(new string('n', 51)), "ValueTooLong", "Name Is Longer Than 50 Characters");
    }

    [Fact]
    public void SyncUpValidator_AcceptsAValidList()
    {
        Assert.True(ValidateSyncUp([TestData.GitIgnoreModel("CSharp"), TestData.GitIgnoreModel("React")]).IsValid);
    }

    [Fact]
    public void SyncUpValidator_RejectsAMissingList()
    {
        AssertSingleError(ValidateSyncUp(null), "ValueRequired", "UploadGitIgnoreFileTypes Is Required");
    }

    [Fact]
    public void SyncUpValidator_AppliesTheGitIgnoreFileRulesToEveryFile()
    {
        AssertSingleError(ValidateSyncUp([TestData.GitIgnoreModel("CSharp", new string('c', 16385))]),
            "ValueTooLong", "CSharp.Content Is Longer Than 16384 Characters");
    }

    [Fact]
    public void SyncUpValidator_RejectsNamesThatDifferOnlyInCase()
    {
        AssertSingleError(ValidateSyncUp([TestData.GitIgnoreModel("CSharp"), TestData.GitIgnoreModel("csharp")]),
            "ValuesNotUnique", "Name Values Are Not Unique");
    }
}
