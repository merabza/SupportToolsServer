using System;
using System.Collections.Generic;
using FluentValidation.Results;
using SupportToolsServer.Application.Projects;
using SupportToolsServer.Application.Projects.UpdateProject;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportToolsServer.Tests.Application.Projects;

public sealed class ProjectValidatorsTests
{
    private static ValidationResult Validate(StsProjectDataModel model)
    {
        return new ProjectModelValidator().Validate(model);
    }

    private static void AssertSingleError(ValidationResult result, string errorCode, string errorMessage)
    {
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(errorMessage, failure.ErrorMessage);
    }

    private static StsProjectDataModel ModelWithReferences()
    {
        return TestData.ProjectModel("AppA", "default", TestData.DatabaseParametersModel("Pc1.Sql", "Reduce", "Backups"),
            TestData.DatabaseParametersModel("Pc1.Sql"), ["RepoA", "RepoB"], ["RepoA"], ["react", "@reduxjs/toolkit"]);
    }

    [Fact]
    public void ModelValidator_AcceptsAProjectWithEveryValue()
    {
        Assert.True(Validate(ModelWithReferences()).IsValid);
    }

    //The client model allows every text but the type to be missing; it stores empty strings as well. The database
    //parameters and every list may be empty
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_AcceptsAProjectWithoutTheOptionalValues(string? value)
    {
        var model = new StsProjectDataModel
        {
            Name = "AppA",
            ProjectType = "Standard",
            ProjectGroupName = value,
            EditorConfigPatternName = value,
            MainProjectName = value,
            ProgramArchiveDateMask = value,
            SolutionFileName = value,
            KeyGuidPart = value,
            DevDatabaseParameters = new StsDatabaseParametersDataModel { DbConnectionName = value }
        };

        Assert.True(Validate(model).IsValid);
    }

    [Fact]
    public void ModelValidator_AcceptsTheMaximumLengths()
    {
        StsProjectDataModel model = ModelWithReferences();
        model.Name = new string('n', 100);
        model.ProjectType = new string('t', 50);
        model.ProjectGroupName = new string('g', 100);
        model.ProjectDescription = new string('d', 255);
        model.EditorConfigPatternName = new string('e', 50);
        model.MainProjectName = new string('m', 100);
        model.ProgramArchiveDateMask = new string('a', 50);
        model.SolutionFileName = new string('s', 260);
        model.KeyGuidPart = new string('k', 256);
        model.DevDatabaseParameters!.DbConnectionName = new string('c', 100);
        model.DevDatabaseParameters.DatabaseName = new string('b', 128);
        model.GitProjectNames = [new string('r', 50)];
        model.FrontNpmPackageNames = [new string('p', 214)];
        model.RedundantFileNames = [new string('f', 260)];
        model.AllowToolsList = [new string('l', 50)];
        model.Endpoints[0].EndpointRoute = new string('o', 256);
        model.RouteClasses[0].Base = new string('u', 256);

        Assert.True(Validate(model).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ModelValidator_RejectsAMissingName(string name)
    {
        AssertSingleError(Validate(TestData.ProjectModel(name)), "ValueRequired", "Name Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsALongerName()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.Name = new string('n', 101);

        AssertSingleError(Validate(model), "ValueTooLong", "Name Is Longer Than 100 Characters");
    }

    //The client always sends the name of its enum; JSON can still send null for it
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ModelValidator_RejectsAMissingProjectTypeNamingTheProject(string? projectType)
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.ProjectType = projectType!;

        AssertSingleError(Validate(model), "ValueRequired", "AppA.ProjectType Is Required");
    }

    [Theory]
    [InlineData(nameof(StsProjectDataModel.ProjectType), 50)]
    [InlineData(nameof(StsProjectDataModel.ProjectGroupName), 100)]
    [InlineData(nameof(StsProjectDataModel.ProjectDescription), 255)]
    [InlineData(nameof(StsProjectDataModel.EditorConfigPatternName), 50)]
    [InlineData(nameof(StsProjectDataModel.MainProjectName), 100)]
    [InlineData(nameof(StsProjectDataModel.ApiContractsProjectName), 100)]
    [InlineData(nameof(StsProjectDataModel.SpaProjectName), 100)]
    [InlineData(nameof(StsProjectDataModel.DbContextName), 100)]
    [InlineData(nameof(StsProjectDataModel.ProjectShortPrefix), 100)]
    [InlineData(nameof(StsProjectDataModel.ScaffoldSeederProjectName), 100)]
    [InlineData(nameof(StsProjectDataModel.DbContextProjectName), 100)]
    [InlineData(nameof(StsProjectDataModel.NewDataSeedingClassLibProjectName), 100)]
    [InlineData(nameof(StsProjectDataModel.ProgramArchiveDateMask), 50)]
    [InlineData(nameof(StsProjectDataModel.ProgramArchiveExtension), 50)]
    [InlineData(nameof(StsProjectDataModel.ParametersFileDateMask), 50)]
    [InlineData(nameof(StsProjectDataModel.ParametersFileExtension), 50)]
    [InlineData(nameof(StsProjectDataModel.ProjectFolderName), 260)]
    [InlineData(nameof(StsProjectDataModel.SolutionFileName), 260)]
    [InlineData(nameof(StsProjectDataModel.ProjectSecurityFolderPath), 260)]
    [InlineData(nameof(StsProjectDataModel.MigrationStartupProjectFilePath), 260)]
    [InlineData(nameof(StsProjectDataModel.MigrationProjectFilePath), 260)]
    [InlineData(nameof(StsProjectDataModel.DataSeederRulesByTableStartupProjectFilePath), 260)]
    [InlineData(nameof(StsProjectDataModel.OldDataConvertorForDataSeeder), 260)]
    [InlineData(nameof(StsProjectDataModel.SeedProjectFilePath), 260)]
    [InlineData(nameof(StsProjectDataModel.SeedProjectParametersFilePath), 260)]
    [InlineData(nameof(StsProjectDataModel.ExcludesRulesParametersFilePath), 260)]
    [InlineData(nameof(StsProjectDataModel.AppSetEnKeysJsonFileName), 260)]
    [InlineData(nameof(StsProjectDataModel.MigrationSqlFilesFolder), 260)]
    [InlineData(nameof(StsProjectDataModel.PrepareProdCopyDatabaseProjectFilePath), 260)]
    [InlineData(nameof(StsProjectDataModel.PrepareProdCopyDatabaseProjectParametersFilePath), 260)]
    [InlineData(nameof(StsProjectDataModel.PairedDbObjectsResultFileName), 260)]
    [InlineData(nameof(StsProjectDataModel.KeyGuidPart), 256)]
    public void ModelValidator_RejectsALongerValueNamingTheProject(string propertyName, int maxLength)
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        typeof(StsProjectDataModel).GetProperty(propertyName)!.SetValue(model, new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong",
            $"AppA.{propertyName} Is Longer Than {maxLength} Characters");
    }

    //The message names the value, never the secret itself
    [Fact]
    public void ModelValidator_RejectsALongerKeyPartWithoutShowingIt()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.KeyGuidPart = TestData.MadeUpKeyGuidPart + new string('k', 256);

        ValidationResult result = Validate(model);

        AssertSingleError(result, "ValueTooLong", "AppA.KeyGuidPart Is Longer Than 256 Characters");
        Assert.DoesNotContain(TestData.MadeUpKeyGuidPart, result.Errors[0].ErrorMessage, StringComparison.Ordinal);
    }

    //Each database parameters part names itself, so the two are told apart
    [Theory]
    [InlineData(nameof(StsDatabaseParametersDataModel.DbConnectionName), 100)]
    [InlineData(nameof(StsDatabaseParametersDataModel.DatabaseRecoveryModel), 50)]
    [InlineData(nameof(StsDatabaseParametersDataModel.DbServerFoldersSetName), 50)]
    [InlineData(nameof(StsDatabaseParametersDataModel.DatabaseName), 128)]
    [InlineData(nameof(StsDatabaseParametersDataModel.SmartSchemaName), 100)]
    [InlineData(nameof(StsDatabaseParametersDataModel.FileStorageName), 100)]
    [InlineData(nameof(StsDatabaseParametersDataModel.BackupNamePrefix), 100)]
    [InlineData(nameof(StsDatabaseParametersDataModel.DateMask), 50)]
    [InlineData(nameof(StsDatabaseParametersDataModel.BackupFileExtension), 50)]
    [InlineData(nameof(StsDatabaseParametersDataModel.BackupNameMiddlePart), 100)]
    [InlineData(nameof(StsDatabaseParametersDataModel.BackupType), 50)]
    public void ModelValidator_RejectsALongerDatabaseParameterNamingThePart(string propertyName, int maxLength)
    {
        StsProjectDataModel model = ModelWithReferences();
        typeof(StsDatabaseParametersDataModel).GetProperty(propertyName)!.SetValue(model.ProdCopyDatabaseParameters,
            new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong",
            $"ProdCopyDatabaseParameters.{propertyName} Is Longer Than {maxLength} Characters");
    }

    [Fact]
    public void ModelValidator_NamesTheDevPart()
    {
        StsProjectDataModel model = ModelWithReferences();
        model.DevDatabaseParameters!.DatabaseName = new string('x', 129);

        AssertSingleError(Validate(model), "ValueTooLong",
            "DevDatabaseParameters.DatabaseName Is Longer Than 128 Characters");
    }

    //The validator of a part is registered in DI as well, so it needs a constructor without parameters
    [Fact]
    public void DatabaseParametersValidator_WithoutAPartName_NamesTheValuesAsDatabaseParameters()
    {
        ValidationResult result = new DatabaseParametersModelValidator().Validate(
            new StsDatabaseParametersDataModel { BackupType = new string('x', 51) });

        AssertSingleError(result, "ValueTooLong", "DatabaseParameters.BackupType Is Longer Than 50 Characters");
    }

    //JSON can still send null for a list; the client sends its missing lists as empty ones
    [Theory]
    [InlineData(nameof(StsProjectDataModel.GitProjectNames))]
    [InlineData(nameof(StsProjectDataModel.ScaffoldSeederGitProjectNames))]
    [InlineData(nameof(StsProjectDataModel.FrontNpmPackageNames))]
    [InlineData(nameof(StsProjectDataModel.RedundantFileNames))]
    [InlineData(nameof(StsProjectDataModel.AllowToolsList))]
    [InlineData(nameof(StsProjectDataModel.Endpoints))]
    [InlineData(nameof(StsProjectDataModel.RouteClasses))]
    public void ModelValidator_RejectsAMissingListNamingTheProject(string propertyName)
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        typeof(StsProjectDataModel).GetProperty(propertyName)!.SetValue(model, null);

        AssertSingleError(Validate(model), "ValueRequired", $"AppA.{propertyName} Is Required");
    }

    [Theory]
    [InlineData(nameof(StsProjectDataModel.GitProjectNames), null)]
    [InlineData(nameof(StsProjectDataModel.ScaffoldSeederGitProjectNames), "")]
    [InlineData(nameof(StsProjectDataModel.FrontNpmPackageNames), " ")]
    [InlineData(nameof(StsProjectDataModel.RedundantFileNames), "")]
    [InlineData(nameof(StsProjectDataModel.AllowToolsList), null)]
    public void ModelValidator_RejectsAMissingNameInAList(string propertyName, string? name)
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        typeof(StsProjectDataModel).GetProperty(propertyName)!.SetValue(model, new List<string> { name! });

        AssertSingleError(Validate(model), "ValueRequired", $"AppA.{propertyName} Is Required");
    }

    [Theory]
    [InlineData(nameof(StsProjectDataModel.GitProjectNames), 50)]
    [InlineData(nameof(StsProjectDataModel.ScaffoldSeederGitProjectNames), 50)]
    [InlineData(nameof(StsProjectDataModel.FrontNpmPackageNames), 214)]
    [InlineData(nameof(StsProjectDataModel.RedundantFileNames), 260)]
    [InlineData(nameof(StsProjectDataModel.AllowToolsList), 50)]
    public void ModelValidator_RejectsALongerNameInAList(string propertyName, int maxLength)
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        typeof(StsProjectDataModel).GetProperty(propertyName)!.SetValue(model,
            new List<string> { new('x', maxLength + 1) });

        AssertSingleError(Validate(model), "ValueTooLong",
            $"AppA.{propertyName} Is Longer Than {maxLength} Characters");
    }

    //A list is a set: a name may be in it once, and the names match without case. One git may be in both git lists
    [Theory]
    [InlineData(nameof(StsProjectDataModel.GitProjectNames))]
    [InlineData(nameof(StsProjectDataModel.ScaffoldSeederGitProjectNames))]
    [InlineData(nameof(StsProjectDataModel.FrontNpmPackageNames))]
    [InlineData(nameof(StsProjectDataModel.RedundantFileNames))]
    [InlineData(nameof(StsProjectDataModel.AllowToolsList))]
    public void ModelValidator_RejectsARepeatedNameInAList(string propertyName)
    {
        StsProjectDataModel model = ModelWithReferences();
        typeof(StsProjectDataModel).GetProperty(propertyName)!.SetValue(model,
            new List<string> { "Same", "Other", "SAME" });

        AssertSingleError(Validate(model), "ValuesNotUnique", $"AppA.{propertyName} Values Are Not Unique");
    }

    [Fact]
    public void ModelValidator_AcceptsTheSameGitInBothGitLists()
    {
        StsProjectDataModel model = ModelWithReferences();
        model.ScaffoldSeederGitProjectNames = ["repoa", "RepoB"];

        Assert.True(Validate(model).IsValid);
    }

    [Fact]
    public void ModelValidator_RejectsAMissingEndpointOrRouteClass()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.Endpoints = [null!];
        model.RouteClasses = [null!];

        ValidationResult result = Validate(model);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "AppA.Endpoints Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "AppA.RouteClasses Is Required");
    }

    //The key of an endpoint or a route class is the key of the client's dictionary, and the keys match without case
    [Fact]
    public void ModelValidator_RejectsARepeatedEndpointOrRouteClassKey()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.Endpoints.Add(new StsProjectEndpointDataModel
        {
            Name = "UPLOAD", HttpMethod = "Get", EndpointType = "Query"
        });
        model.RouteClasses.Add(new StsProjectRouteClassDataModel { Name = "main" });

        ValidationResult result = Validate(model);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValuesNotUnique" && e.ErrorMessage == "AppA.Endpoints.Name Values Are Not Unique");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValuesNotUnique" &&
                 e.ErrorMessage == "AppA.RouteClasses.Name Values Are Not Unique");
    }

    [Theory]
    [InlineData(nameof(StsProjectEndpointDataModel.EndpointName), 100)]
    [InlineData(nameof(StsProjectEndpointDataModel.EndpointRoute), 256)]
    [InlineData(nameof(StsProjectEndpointDataModel.HttpMethod), 50)]
    [InlineData(nameof(StsProjectEndpointDataModel.EndpointType), 50)]
    [InlineData(nameof(StsProjectEndpointDataModel.ReturnType), 256)]
    public void ModelValidator_RejectsALongerEndpointValueNamingTheEndpoint(string propertyName, int maxLength)
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        typeof(StsProjectEndpointDataModel).GetProperty(propertyName)!.SetValue(model.Endpoints[0],
            new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong",
            $"Endpoints.Upload.{propertyName} Is Longer Than {maxLength} Characters");
    }

    //The client always sends the names of its enums; JSON can still send null for them
    [Theory]
    [InlineData(nameof(StsProjectEndpointDataModel.HttpMethod))]
    [InlineData(nameof(StsProjectEndpointDataModel.EndpointType))]
    public void ModelValidator_RejectsAMissingEndpointEnumName(string propertyName)
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        typeof(StsProjectEndpointDataModel).GetProperty(propertyName)!.SetValue(model.Endpoints[0], null);

        AssertSingleError(Validate(model), "ValueRequired", $"Endpoints.Upload.{propertyName} Is Required");
    }

    [Fact]
    public void ModelValidator_RejectsAnEndpointWithoutKeyNamingTheValuesByTheList()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.Endpoints[0].Name = "";
        model.Endpoints[0].ReturnType = new string('x', 257);

        ValidationResult result = Validate(model);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "Endpoints.Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" &&
                 e.ErrorMessage == "Endpoints.ReturnType Is Longer Than 256 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerEndpointKey()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.Endpoints[0].Name = new string('x', 101);

        AssertSingleError(Validate(model), "ValueTooLong", "Endpoints.Name Is Longer Than 100 Characters");
    }

    [Theory]
    [InlineData(nameof(StsProjectRouteClassDataModel.Root), 50)]
    [InlineData(nameof(StsProjectRouteClassDataModel.ApiVersion), 50)]
    [InlineData(nameof(StsProjectRouteClassDataModel.Base), 256)]
    public void ModelValidator_RejectsALongerRouteClassValueNamingTheRouteClass(string propertyName, int maxLength)
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        typeof(StsProjectRouteClassDataModel).GetProperty(propertyName)!.SetValue(model.RouteClasses[0],
            new string('x', maxLength + 1));

        AssertSingleError(Validate(model), "ValueTooLong",
            $"RouteClasses.Main.{propertyName} Is Longer Than {maxLength} Characters");
    }

    [Fact]
    public void ModelValidator_RejectsARouteClassWithoutKeyNamingTheValuesByTheList()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.RouteClasses[0].Name = " ";
        model.RouteClasses[0].Root = new string('x', 51);

        ValidationResult result = Validate(model);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueRequired" && e.ErrorMessage == "RouteClasses.Name Is Required");
        Assert.Contains(result.Errors,
            e => e.ErrorCode == "ValueTooLong" && e.ErrorMessage == "RouteClasses.Root Is Longer Than 50 Characters");
    }

    [Fact]
    public void ModelValidator_RejectsALongerRouteClassKey()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.RouteClasses[0].Name = new string('x', 101);

        AssertSingleError(Validate(model), "ValueTooLong", "RouteClasses.Name Is Longer Than 100 Characters");
    }

    //Without a name the messages of the other values cannot name the project
    [Fact]
    public void ModelValidator_NamesTheValuesOnly_WhenTheNameIsMissing()
    {
        StsProjectDataModel model = TestData.ProjectModel("");
        model.SolutionFileName = new string('x', 261);
        model.AllowToolsList = ["SeedData", "seeddata"];

        ValidationResult result = Validate(model);

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Name Is Required");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "SolutionFileName Is Longer Than 260 Characters");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "AllowToolsList Values Are Not Unique");
    }

    [Fact]
    public void CommandValidator_ValidatesTheProject()
    {
        StsProjectDataModel model = TestData.ProjectModel("AppA");
        model.Name = new string('n', 101);

        ValidationResult result = new UpdateProjectCommandValidator().Validate(new UpdateProjectCommand(model));

        AssertSingleError(result, "ValueTooLong", "Name Is Longer Than 100 Characters");
    }
}
