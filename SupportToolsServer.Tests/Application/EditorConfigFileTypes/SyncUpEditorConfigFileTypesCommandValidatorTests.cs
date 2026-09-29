using System.Collections.Generic;
using FluentValidation.Results;
using SupportToolsServer.Application.EditorConfigFileTypes.SyncUp;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.EditorConfigFileTypes;

public sealed class SyncUpEditorConfigFileTypesCommandValidatorTests
{
    private static ValidationResult Validate(List<StsEditorConfigFileTypeDataModel>? list)
    {
        return new SyncUpEditorConfigFileTypesCommandValidator().Validate(new SyncUpEditorConfigFileTypesCommand(false,
            list!));
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void Validate_AcceptsAValidList()
    {
        Assert.True(Validate([TestData.EditorConfigModel("default"), TestData.EditorConfigModel("BaGetter")]).IsValid);
    }

    //an empty list is valid: without merge it removes every type from the server
    [Fact]
    public void Validate_AcceptsAnEmptyList()
    {
        Assert.True(Validate([]).IsValid);
    }

    [Fact]
    public void Validate_RejectsAMissingList()
    {
        AssertSingleError(Validate(null), "ValueRequired", "UploadEditorConfigFileTypes Is Required");
    }

    [Fact]
    public void Validate_AppliesTheEditorConfigFileRulesToEveryFile()
    {
        List<StsEditorConfigFileTypeDataModel> list =
            [TestData.EditorConfigModel("BaGetter"), TestData.EditorConfigModel("default", new string('c', 65537))];

        AssertSingleError(Validate(list), "ValueTooLong", "default.Content Is Longer Than 65536 Characters");
    }

    [Fact]
    public void Validate_RejectsNamesThatDifferOnlyInCase()
    {
        AssertSingleError(Validate([TestData.EditorConfigModel("default"), TestData.EditorConfigModel("Default")]),
            "ValuesNotUnique", "Name Values Are Not Unique");
    }
}
