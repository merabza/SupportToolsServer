using System;
using FluentValidation.Results;
using SupportToolsServer.Application.StoredFiles;
using SupportToolsServer.Application.StoredFiles.UpdateStoredFile;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.StoredFiles;

//The messages name the field (and the file), never the content
public sealed class StoredFileValidatorsTests
{
    private const string FilePath = @"D:\1WorkSecurity\AppA\appsettings.json";

    private static ValidationResult Validate(StsStoredFileDataModel model)
    {
        return new StoredFileModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(TestData.MadeUpFileContent)]
    public void ModelValidator_AcceptsAFileWithEmptyOrFilledContent(string content)
    {
        Assert.True(Validate(TestData.StoredFileModel(FilePath, content, 3)).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsAPathOfTheMaximumLength()
    {
        string path = @"D:\" + new string('a', 397);

        Assert.Equal(400, path.Length);
        Assert.True(Validate(TestData.StoredFileModel(path)).IsValid);
    }

    [Fact]
    public void ModelValidator_RejectsALongerPath()
    {
        AssertSingleError(Validate(TestData.StoredFileModel(@"D:\" + new string('a', 398))), "ValueTooLong",
            "Path Is Longer Than 400 Characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingPath(string path)
    {
        AssertSingleError(Validate(TestData.StoredFileModel(path)), "ValueRequired", "Path Is Required");
    }

    [Theory]
    [InlineData(@"..\appsettings.json")]
    [InlineData(@"D:\1WorkSecurity\..\appsettings.json")]
    [InlineData(@"\\server\share\appsettings.json")]
    [InlineData("D:/1WorkSecurity/appsettings.json")]
    [InlineData(@"D:\1WorkSecurity\app|settings.json")]
    [InlineData(@"D:\1WorkSecurity\")]
    public void ModelValidator_RejectsAPathThatIsNotAnAbsoluteWindowsFilePath(string path)
    {
        AssertSingleError(Validate(TestData.StoredFileModel(path)), "InvalidFilePath",
            @"Path Is Not A Valid Absolute Windows File Path (X:\...)");
    }

    [Fact]
    public void ModelValidator_RejectsAMissingContentNamingTheFile()
    {
        AssertSingleError(Validate(TestData.StoredFileModel(FilePath, null!)), "ValueRequired",
            @"D:\1WorkSecurity\AppA\appsettings.json.Content Is Required");
    }

    [Fact]
    public void ModelValidator_AcceptsContentOfTheMaximumSize()
    {
        Assert.True(Validate(TestData.StoredFileModel(FilePath, new string('a', 1048576))).IsValid);
    }

    [Fact]
    public void ModelValidator_RejectsLargerContentNamingTheFileButNotTheContent()
    {
        ValidationResult result = Validate(TestData.StoredFileModel(FilePath, new string('a', 1048577)));

        AssertSingleError(result, "ValueTooLarge",
            @"D:\1WorkSecurity\AppA\appsettings.json.Content Is Larger Than 1048576 Bytes");
        Assert.DoesNotContain("aaa", result.Errors[0].ErrorMessage, StringComparison.Ordinal);
    }

    //A Georgian letter is one character but three UTF-8 bytes: the size is the one that Sha256 and Length count
    [Fact]
    public void ModelValidator_MeasuresTheContentInUtf8Bytes()
    {
        Assert.True(Validate(TestData.StoredFileModel(FilePath, new string('ა', 349525))).IsValid);
        AssertSingleError(Validate(TestData.StoredFileModel(FilePath, new string('ა', 349526))), "ValueTooLarge",
            @"D:\1WorkSecurity\AppA\appsettings.json.Content Is Larger Than 1048576 Bytes");
    }

    //Without a path the message of the content cannot name the file
    [Fact]
    public void ModelValidator_NamesOnlyTheContent_WhenThePathIsMissing()
    {
        ValidationResult result = Validate(TestData.StoredFileModel(" ", null!));

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Path Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Content Is Required");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateStoredFileCommandValidator();

        Assert.True(validator.Validate(new UpdateStoredFileCommand(TestData.StoredFileModel(FilePath, "", 2)))
            .IsValid);
        AssertSingleError(
            validator.Validate(new UpdateStoredFileCommand(TestData.StoredFileModel("appsettings.json"))),
            "InvalidFilePath", @"Path Is Not A Valid Absolute Windows File Path (X:\...)");
    }
}
