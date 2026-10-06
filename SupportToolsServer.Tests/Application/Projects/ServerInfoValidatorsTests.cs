using System.Collections.Generic;
using FluentValidation.Results;
using SupportToolsServer.Application.Projects;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.Projects;

//The rules of the server infos of a project. The validator of a server info is registered in DI as well, so its
//messages name the list and the server info by its server and environment, not the project
public sealed class ServerInfoValidatorsTests
{
    private static ValidationResult Validate(StsProjectDataModel model)
    {
        return new ProjectModelValidator().Validate(model);
    }

    private static ValidationResult Validate(StsServerInfoDataModel serverInfo)
    {
        return new ServerInfoModelValidator().Validate(serverInfo);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    private static StsServerInfoDataModel ServerInfoWithEveryValue()
    {
        return TestData.ServerInfoModel("PAZISI", "Prod", "PAZISI.WebAgent",
            TestData.DatabaseParametersModel("Pc1.Sql", "Reduce", "Backups", "App"),
            TestData.DatabaseParametersModel("Pc1.Sql", null, null, "AppNew"));
    }

    [Fact]
    public void ModelValidator_AcceptsAProjectWithServerInfos()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA",
            serverInfos: [ServerInfoWithEveryValue(), TestData.ServerInfoModel("dl360", "Test")]);

        Assert.True(Validate(model).IsValid);
    }

    //The client model allows every value but the server and the environment to be missing; it stores empty strings as
    //well. The tools may be empty
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ServerInfoValidator_AcceptsAServerInfoWithoutTheOptionalValues(string? value)
    {
        var serverInfo = new StsServerInfoDataModel
        {
            ServerName = "PAZISI",
            EnvironmentName = "Prod",
            WebAgentNameForCheck = value,
            ApiVersionId = value,
            AppSettingsJsonSourceFileName = value,
            AppSettingsEncodedJsonFileName = value,
            ServiceUserName = value
        };

        Assert.True(Validate(serverInfo).IsValid);
    }

    [Fact]
    public void ServerInfoValidator_AcceptsTheMaximumLengths()
    {
        var serverInfo = new StsServerInfoDataModel
        {
            ServerName = new string('s', 100),
            EnvironmentName = new string('e', 50),
            WebAgentNameForCheck = new string('w', 100),
            ApiVersionId = new string('v', 50),
            AppSettingsJsonSourceFileName = new string('j', 260),
            AppSettingsEncodedJsonFileName = new string('c', 260),
            ServiceUserName = new string('u', 128),
            AllowToolsList = [new string('t', 50)],
            CurrentDatabaseParameters = new StsDatabaseParametersDataModel { DatabaseName = new string('d', 128) }
        };

        Assert.True(Validate(serverInfo).IsValid);
    }

    //0 is no port, as in the client
    [Theory]
    [InlineData(0)]
    [InlineData(5022)]
    [InlineData(65535)]
    public void ServerInfoValidator_AcceptsAPortInTheRange(int port)
    {
        StsServerInfoDataModel serverInfo = ServerInfoWithEveryValue();
        serverInfo.ServerSidePort = port;

        Assert.True(Validate(serverInfo).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(int.MinValue)]
    public void ServerInfoValidator_RejectsAPortOutOfTheRangeNamingTheServerInfo(int port)
    {
        StsServerInfoDataModel serverInfo = ServerInfoWithEveryValue();
        serverInfo.ServerSidePort = port;

        AssertSingleError(Validate(serverInfo), "ValueOutOfRange",
            "ServerInfos.PAZISI|Prod.ServerSidePort Is Not Between 0 And 65535");
    }

    //The server and the environment are the key of the server info; JSON can still send null for them
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ServerInfoValidator_RejectsAMissingServerOrEnvironment(string? name)
    {
        StsServerInfoDataModel withoutServer = ServerInfoWithEveryValue();
        withoutServer.ServerName = name!;
        StsServerInfoDataModel withoutEnvironment = ServerInfoWithEveryValue();
        withoutEnvironment.EnvironmentName = name!;

        AssertSingleError(Validate(withoutServer), "ValueRequired", "ServerInfos.ServerName Is Required");
        AssertSingleError(Validate(withoutEnvironment), "ValueRequired", "ServerInfos.EnvironmentName Is Required");
    }

    [Fact]
    public void ServerInfoValidator_RejectsALongerServerOrEnvironment()
    {
        StsServerInfoDataModel longerServer = ServerInfoWithEveryValue();
        longerServer.ServerName = new string('s', 101);
        StsServerInfoDataModel longerEnvironment = ServerInfoWithEveryValue();
        longerEnvironment.EnvironmentName = new string('e', 51);

        AssertSingleError(Validate(longerServer), "ValueTooLong",
            "ServerInfos.ServerName Is Longer Than 100 Characters");
        AssertSingleError(Validate(longerEnvironment), "ValueTooLong",
            "ServerInfos.EnvironmentName Is Longer Than 50 Characters");
    }

    [Theory]
    [InlineData(nameof(StsServerInfoDataModel.WebAgentNameForCheck), 100)]
    [InlineData(nameof(StsServerInfoDataModel.ApiVersionId), 50)]
    [InlineData(nameof(StsServerInfoDataModel.AppSettingsJsonSourceFileName), 260)]
    [InlineData(nameof(StsServerInfoDataModel.AppSettingsEncodedJsonFileName), 260)]
    [InlineData(nameof(StsServerInfoDataModel.ServiceUserName), 128)]
    public void ServerInfoValidator_RejectsALongerValueNamingTheServerInfo(string propertyName, int maxLength)
    {
        StsServerInfoDataModel serverInfo = ServerInfoWithEveryValue();
        typeof(StsServerInfoDataModel).GetProperty(propertyName)!.SetValue(serverInfo,
            new string('x', maxLength + 1));

        AssertSingleError(Validate(serverInfo), "ValueTooLong",
            $"ServerInfos.PAZISI|Prod.{propertyName} Is Longer Than {maxLength} Characters");
    }

    //Without its server or environment the messages of the other values cannot name the server info
    [Fact]
    public void ServerInfoValidator_NamesTheValuesByTheListOnly_WhenTheKeyIsMissing()
    {
        StsServerInfoDataModel serverInfo = ServerInfoWithEveryValue();
        serverInfo.EnvironmentName = "";
        serverInfo.ApiVersionId = new string('x', 51);
        serverInfo.CurrentDatabaseParameters!.DatabaseName = new string('d', 129);

        ValidationResult result = Validate(serverInfo);

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "ServerInfos.EnvironmentName Is Required");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "ServerInfos.ApiVersionId Is Longer Than 50 Characters");
        Assert.Contains(result.Errors,
            e => e.ErrorMessage == "ServerInfos.CurrentDatabaseParameters.DatabaseName Is Longer Than 128 Characters");
    }

    //Each database parameters part names itself and its server info, so the parts are told apart
    [Fact]
    public void ServerInfoValidator_NamesTheDatabaseParametersPartsByTheServerInfo()
    {
        StsServerInfoDataModel serverInfo = ServerInfoWithEveryValue();
        serverInfo.CurrentDatabaseParameters!.DbConnectionName = new string('c', 101);
        serverInfo.NewDatabaseParameters!.BackupType = new string('b', 51);

        ValidationResult result = Validate(serverInfo);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage ==
                "ServerInfos.PAZISI|Prod.CurrentDatabaseParameters.DbConnectionName Is Longer Than 100 Characters");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage ==
                "ServerInfos.PAZISI|Prod.NewDatabaseParameters.BackupType Is Longer Than 50 Characters");
    }

    //JSON can still send null for the list; the client sends a missing list as an empty one
    [Fact]
    public void ServerInfoValidator_RejectsAMissingToolList()
    {
        StsServerInfoDataModel serverInfo = ServerInfoWithEveryValue();
        serverInfo.AllowToolsList = null!;

        AssertSingleError(Validate(serverInfo), "ValueRequired", "ServerInfos.PAZISI|Prod.AllowToolsList Is Required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ServerInfoValidator_RejectsAMissingToolName(string? toolName)
    {
        StsServerInfoDataModel serverInfo = ServerInfoWithEveryValue();
        serverInfo.AllowToolsList = ["ProgramUpdater", toolName!];

        AssertSingleError(Validate(serverInfo), "ValueRequired", "ServerInfos.PAZISI|Prod.AllowToolsList Is Required");
    }

    [Fact]
    public void ServerInfoValidator_RejectsALongerToolName()
    {
        StsServerInfoDataModel serverInfo = ServerInfoWithEveryValue();
        serverInfo.AllowToolsList = [new string('t', 51)];

        AssertSingleError(Validate(serverInfo), "ValueTooLong",
            "ServerInfos.PAZISI|Prod.AllowToolsList Is Longer Than 50 Characters");
    }

    //The tools are a set: a name may be in it once, and the names match without case
    [Fact]
    public void ServerInfoValidator_RejectsARepeatedToolName()
    {
        StsServerInfoDataModel serverInfo = ServerInfoWithEveryValue();
        serverInfo.AllowToolsList = ["ProgramUpdater", "ServiceStarter", "PROGRAMUPDATER"];

        AssertSingleError(Validate(serverInfo), "ValuesNotUnique",
            "ServerInfos.PAZISI|Prod.AllowToolsList Values Are Not Unique");
    }

    [Fact]
    public void ModelValidator_RejectsAMissingServerInfoNamingTheProject()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA", serverInfos: [null!]);

        AssertSingleError(Validate(model), "ValueRequired", "AppA.ServerInfos Is Required");
    }

    //The server and the environment together are the key of a server info in its project, and the names match without
    //case
    [Fact]
    public void ModelValidator_RejectsARepeatedServerAndEnvironmentNamingTheProject()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA",
            serverInfos:
            [
                TestData.ServerInfoModel("PAZISI", "Prod"), TestData.ServerInfoModel("dl360", "Prod"),
                TestData.ServerInfoModel("pazisi", "PROD")
            ]);

        AssertSingleError(Validate(model), "ValuesNotUnique",
            "AppA.ServerInfos.ServerName|EnvironmentName Values Are Not Unique");
    }

    //A server may be in a project once in each environment, and an environment once on each server
    [Fact]
    public void ModelValidator_AcceptsTheSameServerInOtherEnvironmentsAndTheSameEnvironmentOnOtherServers()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA",
            serverInfos:
            [
                TestData.ServerInfoModel("PAZISI", "Prod"), TestData.ServerInfoModel("PAZISI", "Test"),
                TestData.ServerInfoModel("dl360", "Prod")
            ]);

        Assert.True(Validate(model).IsValid);
    }

    //Server infos without their key are reported as such, not as a repeated key
    [Fact]
    public void ModelValidator_RejectsServerInfosWithoutAServerOnlyForTheMissingServer()
    {
        StsServerInfoDataModel first = TestData.ServerInfoModel("PAZISI", "Prod");
        first.ServerName = "";
        StsServerInfoDataModel second = TestData.ServerInfoModel("PAZISI", "Prod");
        second.ServerName = null!;
        StsProjectDataModel model = TestData.ProjectModel("AppA", serverInfos: [first, second]);

        ValidationResult result = Validate(model);

        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, e =>
        {
            Assert.Equal("ValueRequired", e.ErrorCode);
            Assert.Equal("ServerInfos.ServerName Is Required", e.ErrorMessage);
        });
    }

    //The messages of a server info in a project come from its own validator
    [Fact]
    public void ModelValidator_ValidatesEveryServerInfo()
    {
        List<StsServerInfoDataModel> serverInfos =
            [TestData.ServerInfoModel("PAZISI", "Prod"), TestData.ServerInfoModel("dl360", "Test")];
        serverInfos[1].ServerSidePort = 70000;
        StsProjectDataModel model = TestData.ProjectModel("AppA", serverInfos: serverInfos);

        AssertSingleError(Validate(model), "ValueOutOfRange",
            "ServerInfos.dl360|Test.ServerSidePort Is Not Between 0 And 65535");
    }
}
