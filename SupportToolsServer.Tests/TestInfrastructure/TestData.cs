using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.DotnetTools;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Primitives;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SupportToolsServerCore.Domain.Runtimes;

namespace SupportToolsServer.Tests.TestInfrastructure;

internal static class TestData
{
    public static EditorConfigFileType NewEditorConfigFileType(string name, string content = "root = true",
        int version = EntityVersion.Initial)
    {
        return new EditorConfigFileType(EditorConfigFileTypeId.CreateUnique(), name, content, version);
    }

    public static StsEditorConfigFileTypeDataModel EditorConfigModel(string name, string content = "root = true")
    {
        return new StsEditorConfigFileTypeDataModel { Name = name, Content = content };
    }

    public static GitIgnoreFileType NewGitIgnoreFileType(string name, string content = "bin/",
        int version = EntityVersion.Initial)
    {
        return new GitIgnoreFileType(GitIgnoreFileTypeId.CreateUnique(), name, content, version);
    }

    public static GitRepo NewGitRepo(string name, GitIgnoreFileType gitIgnoreFileType, string? address = null,
        int version = EntityVersion.Initial)
    {
        return new GitRepo(GitRepoId.CreateUnique(), name, address ?? AddressOf(name), name, gitIgnoreFileType.Id,
            version);
    }

    public static DeploymentEnvironment NewEnvironment(string name, string? description = null,
        int version = EntityVersion.Initial)
    {
        return new DeploymentEnvironment(DeploymentEnvironmentId.CreateUnique(), name, description, version);
    }

    //version is the expected version of an upsert: 0 creates the record
    public static StsEnvironmentDataModel EnvironmentModel(string name, string? description = null, int version = 0)
    {
        return new StsEnvironmentDataModel { Name = name, Description = description, Version = version };
    }

    public static Runtime NewRuntime(string name, string? description = null, int version = EntityVersion.Initial)
    {
        return new Runtime(RuntimeId.CreateUnique(), name, description, version);
    }

    public static StsRuntimeDataModel RuntimeModel(string name, string? description = null, int version = 0)
    {
        return new StsRuntimeDataModel { Name = name, Description = description, Version = version };
    }

    public static NpmPackage NewNpmPackage(string name, string? description = null, int version = EntityVersion.Initial)
    {
        return new NpmPackage(NpmPackageId.CreateUnique(), name, description, version);
    }

    public static StsNpmPackageDataModel NpmPackageModel(string name, string? description = null, int version = 0)
    {
        return new StsNpmPackageDataModel { Name = name, Description = description, Version = version };
    }

    public static ReactAppTemplate NewReactAppTemplate(string name, string template = "typescript",
        int version = EntityVersion.Initial)
    {
        return new ReactAppTemplate(ReactAppTemplateId.CreateUnique(), name, template, version);
    }

    public static StsReactAppTemplateDataModel ReactAppTemplateModel(string name, string template = "typescript",
        int version = 0)
    {
        return new StsReactAppTemplateDataModel { Name = name, Template = template, Version = version };
    }

    public static DotnetTool NewDotnetTool(string name, string packageId = "dotnet-ef", string? maxVersion = null,
        string? description = null, int version = EntityVersion.Initial)
    {
        return new DotnetTool(DotnetToolId.CreateUnique(), name, packageId, maxVersion, description, version);
    }

    public static StsDotnetToolDataModel DotnetToolModel(string name, string packageId = "dotnet-ef",
        string? maxVersion = null, string? description = null, int version = 0)
    {
        return new StsDotnetToolDataModel
        {
            Name = name,
            PackageId = packageId,
            MaxVersion = maxVersion,
            Description = description,
            Version = version
        };
    }

    public static StsGitDataModel GitModel(string name, string gitIgnorePatternName, string? address = null)
    {
        return new StsGitDataModel
        {
            GitProjectName = name,
            GitProjectAddress = address ?? AddressOf(name),
            GitProjectFolderName = name,
            GitIgnorePatternName = gitIgnorePatternName
        };
    }

    public static StsGitIgnoreFileTypeDataModel GitIgnoreModel(string name, string content = "bin/")
    {
        return new StsGitIgnoreFileTypeDataModel { Name = name, Content = content };
    }

    public static string AddressOf(string gitName)
    {
        return $"git@github.com:test/{gitName}.git";
    }
}
