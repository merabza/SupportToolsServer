using System;
using FluentValidation.Results;
using SupportToolsServer.Application.ApiClients;
using SupportToolsServer.Application.ApiClients.UpdateApiClient;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.ApiClients;

public sealed class ApiClientValidatorsTests
{
    private static ValidationResult Validate(StsApiClientDataModel model)
    {
        return new ApiClientModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    [Fact]
    public void ModelValidator_AcceptsAnApiClientWithEveryValue()
    {
        Assert.True(Validate(TestData.ApiClientModel("Pc1.WebAgent")).IsValid);
    }

    //The client model allows both values to be missing; it stores empty strings as well
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_AcceptsAnApiClientWithoutServerAndApiKey(string? value)
    {
        Assert.True(Validate(TestData.ApiClientModel("Pc1.WebAgent", value, value)).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        Assert.True(Validate(TestData.ApiClientModel(new string('n', 100), new string('s', 256), new string('k', 256)))
            .IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.ApiClientModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        AssertSingleError(Validate(TestData.ApiClientModel(new string('n', 101))), "ValueTooLong",
            "Name Is Longer Than 100 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerServerNamingTheApiClient()
    {
        AssertSingleError(Validate(TestData.ApiClientModel("Pc1.WebAgent", new string('s', 257))), "ValueTooLong",
            "Pc1.WebAgent.Server Is Longer Than 256 Characters");
    }

    //The message names the value, never the key itself
    [Fact]
    public void ModelValidator_RejectsALongerApiKeyNamingTheApiClientButNotTheKey()
    {
        string apiKey = TestData.MadeUpApiKey + new string('k', 256);

        ValidationResult result = Validate(TestData.ApiClientModel("Pc1.WebAgent", apiKey: apiKey));

        AssertSingleError(result, "ValueTooLong", "Pc1.WebAgent.ApiKey Is Longer Than 256 Characters");
        Assert.DoesNotContain(TestData.MadeUpApiKey, result.Errors[0].ErrorMessage, StringComparison.Ordinal);
    }

    //Without a name the messages of the other values cannot name the API client
    [Fact]
    public void ModelValidator_NamesOnlyTheValues_WhenTheNameIsMissing()
    {
        ValidationResult result =
            Validate(TestData.ApiClientModel(" ", new string('s', 257), new string('k', 257)));

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "Server Is Longer Than 256 Characters");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "ApiKey Is Longer Than 256 Characters");
    }

    [Fact]
    public void UpdateValidator_AppliesTheModelRules()
    {
        var validator = new UpdateApiClientCommandValidator();

        Assert.True(validator.Validate(new UpdateApiClientCommand(TestData.ApiClientModel("Pc1.WebAgent", version: 3)))
            .IsValid);
        AssertSingleError(
            validator.Validate(new UpdateApiClientCommand(TestData.ApiClientModel("Pc1.WebAgent",
                new string('s', 257)))), "ValueTooLong", "Pc1.WebAgent.Server Is Longer Than 256 Characters");
    }
}
