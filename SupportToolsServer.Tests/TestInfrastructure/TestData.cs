using System.Collections.Generic;
using System.Linq;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.DotnetTools;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Primitives;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Tests.TestInfrastructure;

//The secrets of the resources (passwords, API keys, users, the MediatR license key) are made up
internal static class TestData
{
    public const string MadeUpApiKey = "made-up-api-key";
    public const string MadeUpUser = "made-up-user";
    public const string MadeUpPassword = "made-up-password";
    public const string MadeUpLicenseKey = "made-up-license-key";

    //The singleton with its fixed key. The references are the records given, the exchange parameters included
    public static GlobalSettings NewGlobalSettings(FileStorage? fileStorageForExchange = null,
        SmartSchema? smartSchemaForExchange = null, SmartSchema? smartSchemaForLocal = null,
        ApiClient? localPackageManagerWebApiClient = null, FileStorage? exchangeFileStorage = null,
        SmartSchema? exchangeSmartSchema = null, SmartSchema? localSmartSchema = null,
        int version = EntityVersion.Initial)
    {
        return new GlobalSettings(GlobalSettingsId.Singleton, "ltgmz", ".up!", "yyyyMMddHHmmss", ".zip", "yyyyMMdd",
            ".json", MadeUpLicenseKey, fileStorageForExchange?.Id, smartSchemaForExchange?.Id, smartSchemaForLocal?.Id,
            localPackageManagerWebApiClient?.Id,
            new DatabasesBackupFilesExchange(".down!", ".up!", exchangeFileStorage?.Id, exchangeSmartSchema?.Id,
                localSmartSchema?.Id), version);
    }

    //version is the expected version of an upsert: 0 creates the singleton
    public static StsGlobalSettingsDataModel GlobalSettingsModel(string? fileStorageNameForExchange = null,
        string? smartSchemaNameForExchange = null, string? smartSchemaNameForLocal = null,
        string? localPackageManagerWebApiClientName = null, string? exchangeFileStorageName = null,
        string? exchangeSmartSchemaName = null, string? localSmartSchemaName = null, int version = 0)
    {
        return new StsGlobalSettingsDataModel
        {
            ServiceDescriptionSignature = "ltgmz",
            UploadTempExtension = ".up!",
            ProgramArchiveDateMask = "yyyyMMddHHmmss",
            ProgramArchiveExtension = ".zip",
            ParametersFileDateMask = "yyyyMMdd",
            ParametersFileExtension = ".json",
            MediatRLicenseKey = MadeUpLicenseKey,
            FileStorageNameForExchange = fileStorageNameForExchange,
            SmartSchemaNameForExchange = smartSchemaNameForExchange,
            SmartSchemaNameForLocal = smartSchemaNameForLocal,
            LocalPackageManagerWebApiClientName = localPackageManagerWebApiClientName,
            DatabasesBackupFilesExchange = new StsDatabasesBackupFilesExchangeDataModel
            {
                DownloadTempExtension = ".down!",
                UploadTempExtension = ".up!",
                ExchangeFileStorageName = exchangeFileStorageName,
                ExchangeSmartSchemaName = exchangeSmartSchemaName,
                LocalSmartSchemaName = localSmartSchemaName
            },
            Version = version
        };
    }

    //The singleton with its fixed key and the references to the records given
    public static ProjectCreatorSettings NewProjectCreatorSettings(Server? productionServer = null,
        DeploymentEnvironment? productionEnvironment = null, DatabaseServerConnection? developerDbConnection = null,
        FileStorage? databaseExchangeFileStorage = null, SmartSchema? useSmartSchema = null,
        int version = EntityVersion.Initial)
    {
        return new ProjectCreatorSettings(ProjectCreatorSettingsId.Singleton, 4, "FakeHost", @"D:\1WorkDotnet",
            @"D:\1WorkSecurity", productionServer?.Id, productionEnvironment?.Id, developerDbConnection?.Id,
            databaseExchangeFileStorage?.Id, useSmartSchema?.Id, version);
    }

    //version is the expected version of an upsert: 0 creates the singleton
    public static StsProjectCreatorSettingsDataModel ProjectCreatorSettingsModel(string? productionServerName = null,
        string? productionEnvironmentName = null, string? developerDbConnectionName = null,
        string? databaseExchangeFileStorageName = null, string? useSmartSchema = null, int version = 0)
    {
        return new StsProjectCreatorSettingsDataModel
        {
            IndentSize = 4,
            FakeHostProjectName = "FakeHost",
            ProjectsFolderPathReal = @"D:\1WorkDotnet",
            SecretsFolderPathReal = @"D:\1WorkSecurity",
            ProductionServerName = productionServerName,
            ProductionEnvironmentName = productionEnvironmentName,
            DeveloperDbConnectionName = developerDbConnectionName,
            DatabaseExchangeFileStorageName = databaseExchangeFileStorageName,
            UseSmartSchema = useSmartSchema,
            Version = version
        };
    }

    public static ProjectTemplate NewProjectTemplate(string name, ReactAppTemplate? reactTemplate = null,
        int version = EntityVersion.Initial)
    {
        return new ProjectTemplate(ProjectTemplateId.CreateUnique(), name, "Api", "ReactTest", "Rt", true, false, true,
            false, true, false, true, false, true, false, reactTemplate?.Id, version);
    }

    //version is the expected version of an upsert: 0 creates the record
    public static StsProjectTemplateDataModel ProjectTemplateModel(string name, string? reactTemplateName = null,
        int version = 0)
    {
        return new StsProjectTemplateDataModel
        {
            Name = name,
            SupportProjectType = "Api",
            TestProjectName = "ReactTest",
            TestProjectShortName = "Rt",
            UseDatabase = true,
            UseMenu = true,
            UseReact = true,
            UseIdentity = true,
            UseSignalR = true,
            ReactTemplateName = reactTemplateName,
            Version = version
        };
    }

    //The details come with the update that gives the stored version, as the constructor takes none
    public static SmartSchema NewSmartSchema(string name, int lastPreserveCount = 1,
        IEnumerable<(string PeriodType, int PreserveCount)>? details = null, int version = EntityVersion.Initial)
    {
        var smartSchema = new SmartSchema(SmartSchemaId.CreateUnique(), name, lastPreserveCount, version - 1);
        smartSchema.Update(name, lastPreserveCount,
            (details ?? []).Select(x => SmartSchemaDetail.Create(x.PeriodType, x.PreserveCount)));
        return smartSchema;
    }

    //version is the expected version of an upsert: 0 creates the record
    public static StsSmartSchemaDataModel SmartSchemaModel(string name, int lastPreserveCount = 1,
        IEnumerable<(string PeriodType, int PreserveCount)>? details = null, int version = 0)
    {
        return new StsSmartSchemaDataModel
        {
            Name = name,
            LastPreserveCount = lastPreserveCount,
            Details =
            [
                .. (details ?? []).Select(x => new StsSmartSchemaDetailDataModel
                {
                    PeriodType = x.PeriodType, PreserveCount = x.PreserveCount
                })
            ],
            Version = version
        };
    }

    public static FileStorage NewFileStorage(string name, string? fileStoragePath = "ftp://ftp.example.com/x/",
        string? userName = MadeUpUser, string? password = MadeUpPassword, int version = EntityVersion.Initial)
    {
        return new FileStorage(FileStorageId.CreateUnique(), name, fileStoragePath, userName, password, 255, 4, 1,
            version);
    }

    public static StsFileStorageDataModel FileStorageModel(string name,
        string? fileStoragePath = "ftp://ftp.example.com/x/", string? userName = MadeUpUser,
        string? password = MadeUpPassword, int version = 0)
    {
        return new StsFileStorageDataModel
        {
            Name = name,
            FileStoragePath = fileStoragePath,
            UserName = userName,
            Password = password,
            FileNameMaxLength = 255,
            FileSizeSplitPositionInRow = 4,
            FtpSiteLsFileOffset = 1,
            Version = version
        };
    }

    public static ApiClient NewApiClient(string name, string? server = "http://localhost:5031/api/v1/",
        string? apiKey = MadeUpApiKey, int version = EntityVersion.Initial)
    {
        return new ApiClient(ApiClientId.CreateUnique(), name, server, apiKey, version);
    }

    public static StsApiClientDataModel ApiClientModel(string name, string? server = "http://localhost:5031/api/v1/",
        string? apiKey = MadeUpApiKey, int version = 0)
    {
        return new StsApiClientDataModel { Name = name, Server = server, ApiKey = apiKey, Version = version };
    }

    //The folders sets come with the update that gives the stored version, as the constructor takes none
    public static DatabaseServerConnection NewDatabaseServerConnection(string name, ApiClient? dbWebAgent = null,
        IEnumerable<string>? foldersSetNames = null, int version = EntityVersion.Initial)
    {
        var connection = new DatabaseServerConnection(DatabaseServerConnectionId.CreateUnique(), name, "SqlServer",
            null, null, null, false, null, null, false, 0, false, version - 1);
        connection.Update(name, "SqlServer", dbWebAgent?.Id, "Main", "pc1", true, MadeUpUser, MadeUpPassword, true, 30,
            true, (foldersSetNames ?? []).Select(x => DatabaseFoldersSet.Create(x, $@"D:\{x}\Bak", $@"D:\{x}\Data",
                $@"D:\{x}\Log")));
        return connection;
    }

    public static StsDatabaseServerConnectionDataModel DatabaseServerConnectionModel(string name,
        string? dbWebAgentName = null, IEnumerable<string>? foldersSetNames = null, int version = 0)
    {
        return new StsDatabaseServerConnectionDataModel
        {
            Name = name,
            DatabaseServerProvider = "SqlServer",
            DbWebAgentName = dbWebAgentName,
            RemoteDbConnectionName = "Main",
            ServerAddress = "pc1",
            WindowsNtIntegratedSecurity = true,
            ServerUser = MadeUpUser,
            ServerPass = MadeUpPassword,
            TrustServerCertificate = true,
            ConnectionTimeOut = 30,
            Encrypt = true,
            DatabaseFoldersSets =
            [
                .. (foldersSetNames ?? []).Select(x => new StsDatabaseFoldersSetDataModel
                {
                    Name = x, Backup = $@"D:\{x}\Bak", Data = $@"D:\{x}\Data", DataLog = $@"D:\{x}\Log"
                })
            ],
            Version = version
        };
    }

    public static Server NewServer(string name, ApiClient? webAgent = null, ApiClient? webAgentInstaller = null,
        Runtime? runtime = null, int version = EntityVersion.Initial)
    {
        return new Server(ServerId.CreateUnique(), name, webAgent?.Id, webAgentInstaller?.Id, "deployer", "deployers",
            runtime?.Id, "/home/deployer/Download", "/opt/apps", version);
    }

    //version is the expected version of an upsert: 0 creates the record
    public static StsServerDataModel ServerModel(string name, string? webAgentName = null,
        string? webAgentInstallerName = null, string? runtime = null, int version = 0)
    {
        return new StsServerDataModel
        {
            Name = name,
            WebAgentName = webAgentName,
            WebAgentInstallerName = webAgentInstallerName,
            FilesUserName = "deployer",
            FilesUsersGroupName = "deployers",
            Runtime = runtime,
            ServerSideDownloadFolder = "/home/deployer/Download",
            ServerSideDeployFolder = "/opt/apps",
            Version = version
        };
    }

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
