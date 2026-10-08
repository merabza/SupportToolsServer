IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ApiClients] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Server] nvarchar(256) NULL,
        [ApiKey] nvarchar(256) NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ApiClients] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [DotnetTools] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [PackageId] nvarchar(100) NOT NULL,
        [MaxVersion] nvarchar(64) NULL,
        [Description] nvarchar(255) NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_DotnetTools] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [EditorConfigFileTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_EditorConfigFileTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [Environments] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(255) NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Environments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [FileStorages] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [FileStoragePath] nvarchar(260) NULL,
        [UserName] nvarchar(128) NULL,
        [Password] nvarchar(256) NULL,
        [FileNameMaxLength] int NOT NULL,
        [FileSizeSplitPositionInRow] int NOT NULL,
        [FtpSiteLsFileOffset] int NOT NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_FileStorages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [GitIgnoreFileTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_GitIgnoreFileTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [NpmPackages] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(214) NOT NULL,
        [Description] nvarchar(255) NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_NpmPackages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ReactAppTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Template] nvarchar(214) NOT NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ReactAppTemplates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [Runtimes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(255) NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Runtimes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [SmartSchemas] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [LastPreserveCount] int NOT NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_SmartSchemas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [StoredFiles] (
        [Id] uniqueidentifier NOT NULL,
        [Path] nvarchar(400) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Sha256] nvarchar(64) NOT NULL,
        [Length] int NOT NULL,
        [UpdatedAtUtc] datetime NOT NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_StoredFiles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [DatabaseServerConnections] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DatabaseServerProvider] nvarchar(50) NOT NULL,
        [DbWebAgentId] uniqueidentifier NULL,
        [RemoteDbConnectionName] nvarchar(100) NULL,
        [ServerAddress] nvarchar(256) NULL,
        [WindowsNtIntegratedSecurity] bit NOT NULL,
        [ServerUser] nvarchar(128) NULL,
        [ServerPass] nvarchar(256) NULL,
        [TrustServerCertificate] bit NOT NULL,
        [ConnectionTimeOut] int NOT NULL,
        [Encrypt] bit NOT NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_DatabaseServerConnections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DatabaseServerConnections_ApiClients_DbWebAgentId] FOREIGN KEY ([DbWebAgentId]) REFERENCES [ApiClients] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [GitRepos] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Address] nvarchar(256) NOT NULL,
        [FolderName] nvarchar(100) NOT NULL,
        [GitIgnoreFileTypeId] uniqueidentifier NOT NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_GitRepos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GitRepos_GitIgnoreFileTypes_GitIgnoreFileTypeId] FOREIGN KEY ([GitIgnoreFileTypeId]) REFERENCES [GitIgnoreFileTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ProjectTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [SupportProjectType] nvarchar(50) NOT NULL,
        [TestProjectName] nvarchar(100) NULL,
        [TestProjectShortName] nvarchar(100) NULL,
        [UseDatabase] bit NOT NULL,
        [UseDbPartFolderForDatabaseProjects] bit NOT NULL,
        [UseMenu] bit NOT NULL,
        [UseHttps] bit NOT NULL,
        [UseReact] bit NOT NULL,
        [UseCarcass] bit NOT NULL,
        [UseIdentity] bit NOT NULL,
        [UseReCounter] bit NOT NULL,
        [UseSignalR] bit NOT NULL,
        [UseFluentValidation] bit NOT NULL,
        [ReactTemplateId] uniqueidentifier NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ProjectTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectTemplates_ReactAppTemplates_ReactTemplateId] FOREIGN KEY ([ReactTemplateId]) REFERENCES [ReactAppTemplates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [Servers] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [WebAgentId] uniqueidentifier NULL,
        [WebAgentInstallerId] uniqueidentifier NULL,
        [FilesUserName] nvarchar(128) NULL,
        [FilesUsersGroupName] nvarchar(128) NULL,
        [RuntimeId] uniqueidentifier NULL,
        [ServerSideDownloadFolder] nvarchar(260) NULL,
        [ServerSideDeployFolder] nvarchar(260) NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Servers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Servers_ApiClients_WebAgentId] FOREIGN KEY ([WebAgentId]) REFERENCES [ApiClients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Servers_ApiClients_WebAgentInstallerId] FOREIGN KEY ([WebAgentInstallerId]) REFERENCES [ApiClients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Servers_Runtimes_RuntimeId] FOREIGN KEY ([RuntimeId]) REFERENCES [Runtimes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [GlobalSettings] (
        [Id] uniqueidentifier NOT NULL,
        [ServiceDescriptionSignature] nvarchar(100) NULL,
        [UploadTempExtension] nvarchar(50) NULL,
        [ProgramArchiveDateMask] nvarchar(50) NULL,
        [ProgramArchiveExtension] nvarchar(50) NULL,
        [ParametersFileDateMask] nvarchar(50) NULL,
        [ParametersFileExtension] nvarchar(50) NULL,
        [MediatRLicenseKey] nvarchar(4000) NULL,
        [FileStorageForExchangeId] uniqueidentifier NULL,
        [SmartSchemaForExchangeId] uniqueidentifier NULL,
        [SmartSchemaForLocalId] uniqueidentifier NULL,
        [LocalPackageManagerWebApiClientId] uniqueidentifier NULL,
        [DatabasesBackupFilesExchange_DownloadTempExtension] nvarchar(50) NULL,
        [DatabasesBackupFilesExchange_UploadTempExtension] nvarchar(50) NULL,
        [DatabasesBackupFilesExchange_ExchangeFileStorageId] uniqueidentifier NULL,
        [DatabasesBackupFilesExchange_ExchangeSmartSchemaId] uniqueidentifier NULL,
        [DatabasesBackupFilesExchange_LocalSmartSchemaId] uniqueidentifier NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_GlobalSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GlobalSettings_Singleton] CHECK ([Id] = '00000000-0000-0000-0000-000000000001'),
        CONSTRAINT [FK_GlobalSettings_ApiClients_LocalPackageManagerWebApiClientId] FOREIGN KEY ([LocalPackageManagerWebApiClientId]) REFERENCES [ApiClients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GlobalSettings_FileStorages_DatabasesBackupFilesExchange_ExchangeFileStorageId] FOREIGN KEY ([DatabasesBackupFilesExchange_ExchangeFileStorageId]) REFERENCES [FileStorages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GlobalSettings_FileStorages_FileStorageForExchangeId] FOREIGN KEY ([FileStorageForExchangeId]) REFERENCES [FileStorages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GlobalSettings_SmartSchemas_DatabasesBackupFilesExchange_ExchangeSmartSchemaId] FOREIGN KEY ([DatabasesBackupFilesExchange_ExchangeSmartSchemaId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GlobalSettings_SmartSchemas_DatabasesBackupFilesExchange_LocalSmartSchemaId] FOREIGN KEY ([DatabasesBackupFilesExchange_LocalSmartSchemaId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GlobalSettings_SmartSchemas_SmartSchemaForExchangeId] FOREIGN KEY ([SmartSchemaForExchangeId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GlobalSettings_SmartSchemas_SmartSchemaForLocalId] FOREIGN KEY ([SmartSchemaForLocalId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [SmartSchemaDetails] (
        [Id] uniqueidentifier NOT NULL,
        [PeriodType] nvarchar(50) NOT NULL,
        [PreserveCount] int NOT NULL,
        [SmartSchemaId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_SmartSchemaDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SmartSchemaDetails_SmartSchemas_SmartSchemaId] FOREIGN KEY ([SmartSchemaId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [DatabaseFoldersSets] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Backup] nvarchar(260) NULL,
        [Data] nvarchar(260) NULL,
        [DataLog] nvarchar(260) NULL,
        [DatabaseServerConnectionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_DatabaseFoldersSets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DatabaseFoldersSets_DatabaseServerConnections_DatabaseServerConnectionId] FOREIGN KEY ([DatabaseServerConnectionId]) REFERENCES [DatabaseServerConnections] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [Projects] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ProjectType] nvarchar(50) NOT NULL,
        [ProjectGroupName] nvarchar(100) NULL,
        [ProjectDescription] nvarchar(255) NULL,
        [MajorVersion] int NOT NULL,
        [MinorVersion] int NOT NULL,
        [UseAlternativeWebAgent] bit NOT NULL,
        [EditorConfigFileTypeId] uniqueidentifier NULL,
        [MainProjectName] nvarchar(100) NULL,
        [ApiContractsProjectName] nvarchar(100) NULL,
        [SpaProjectName] nvarchar(100) NULL,
        [DbContextName] nvarchar(100) NULL,
        [ProjectShortPrefix] nvarchar(100) NULL,
        [ScaffoldSeederProjectName] nvarchar(100) NULL,
        [DbContextProjectName] nvarchar(100) NULL,
        [NewDataSeedingClassLibProjectName] nvarchar(100) NULL,
        [ProgramArchiveDateMask] nvarchar(50) NULL,
        [ProgramArchiveExtension] nvarchar(50) NULL,
        [ParametersFileDateMask] nvarchar(50) NULL,
        [ParametersFileExtension] nvarchar(50) NULL,
        [ProjectFolderName] nvarchar(260) NULL,
        [SolutionFileName] nvarchar(260) NULL,
        [ProjectSecurityFolderPath] nvarchar(260) NULL,
        [MigrationStartupProjectFilePath] nvarchar(260) NULL,
        [MigrationProjectFilePath] nvarchar(260) NULL,
        [DataSeederRulesByTableStartupProjectFilePath] nvarchar(260) NULL,
        [OldDataConvertorForDataSeeder] nvarchar(260) NULL,
        [SeedProjectFilePath] nvarchar(260) NULL,
        [SeedProjectParametersFilePath] nvarchar(260) NULL,
        [ExcludesRulesParametersFilePath] nvarchar(260) NULL,
        [AppSetEnKeysJsonFileName] nvarchar(260) NULL,
        [MigrationSqlFilesFolder] nvarchar(260) NULL,
        [PrepareProdCopyDatabaseProjectFilePath] nvarchar(260) NULL,
        [PrepareProdCopyDatabaseProjectParametersFilePath] nvarchar(260) NULL,
        [PairedDbObjectsResultFileName] nvarchar(260) NULL,
        [KeyGuidPart] nvarchar(256) NULL,
        [DevDatabaseParameters_DbConnectionId] uniqueidentifier NULL,
        [DevDatabaseParameters_DatabaseRecoveryModel] nvarchar(50) NULL,
        [DevDatabaseParameters_DbServerFoldersSetName] nvarchar(50) NULL,
        [DevDatabaseParameters_DatabaseName] nvarchar(128) NULL,
        [DevDatabaseParameters_SmartSchemaId] uniqueidentifier NULL,
        [DevDatabaseParameters_FileStorageId] uniqueidentifier NULL,
        [DevDatabaseParameters_CommandTimeOut] int NULL,
        [DevDatabaseParameters_SkipBackupBeforeRestore] bit NULL,
        [DevDatabaseParameters_BackupNamePrefix] nvarchar(100) NULL,
        [DevDatabaseParameters_DateMask] nvarchar(50) NULL,
        [DevDatabaseParameters_BackupFileExtension] nvarchar(50) NULL,
        [DevDatabaseParameters_BackupNameMiddlePart] nvarchar(100) NULL,
        [DevDatabaseParameters_Compress] bit NULL,
        [DevDatabaseParameters_Verify] bit NULL,
        [DevDatabaseParameters_BackupType] nvarchar(50) NULL,
        [ProdCopyDatabaseParameters_DbConnectionId] uniqueidentifier NULL,
        [ProdCopyDatabaseParameters_DatabaseRecoveryModel] nvarchar(50) NULL,
        [ProdCopyDatabaseParameters_DbServerFoldersSetName] nvarchar(50) NULL,
        [ProdCopyDatabaseParameters_DatabaseName] nvarchar(128) NULL,
        [ProdCopyDatabaseParameters_SmartSchemaId] uniqueidentifier NULL,
        [ProdCopyDatabaseParameters_FileStorageId] uniqueidentifier NULL,
        [ProdCopyDatabaseParameters_CommandTimeOut] int NULL,
        [ProdCopyDatabaseParameters_SkipBackupBeforeRestore] bit NULL,
        [ProdCopyDatabaseParameters_BackupNamePrefix] nvarchar(100) NULL,
        [ProdCopyDatabaseParameters_DateMask] nvarchar(50) NULL,
        [ProdCopyDatabaseParameters_BackupFileExtension] nvarchar(50) NULL,
        [ProdCopyDatabaseParameters_BackupNameMiddlePart] nvarchar(100) NULL,
        [ProdCopyDatabaseParameters_Compress] bit NULL,
        [ProdCopyDatabaseParameters_Verify] bit NULL,
        [ProdCopyDatabaseParameters_BackupType] nvarchar(50) NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Projects_DatabaseServerConnections_DevDatabaseParameters_DbConnectionId] FOREIGN KEY ([DevDatabaseParameters_DbConnectionId]) REFERENCES [DatabaseServerConnections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Projects_DatabaseServerConnections_ProdCopyDatabaseParameters_DbConnectionId] FOREIGN KEY ([ProdCopyDatabaseParameters_DbConnectionId]) REFERENCES [DatabaseServerConnections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Projects_EditorConfigFileTypes_EditorConfigFileTypeId] FOREIGN KEY ([EditorConfigFileTypeId]) REFERENCES [EditorConfigFileTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Projects_FileStorages_DevDatabaseParameters_FileStorageId] FOREIGN KEY ([DevDatabaseParameters_FileStorageId]) REFERENCES [FileStorages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Projects_FileStorages_ProdCopyDatabaseParameters_FileStorageId] FOREIGN KEY ([ProdCopyDatabaseParameters_FileStorageId]) REFERENCES [FileStorages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Projects_SmartSchemas_DevDatabaseParameters_SmartSchemaId] FOREIGN KEY ([DevDatabaseParameters_SmartSchemaId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Projects_SmartSchemas_ProdCopyDatabaseParameters_SmartSchemaId] FOREIGN KEY ([ProdCopyDatabaseParameters_SmartSchemaId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ProjectCreatorSettings] (
        [Id] uniqueidentifier NOT NULL,
        [IndentSize] int NOT NULL,
        [FakeHostProjectName] nvarchar(100) NULL,
        [ProjectsFolderPathReal] nvarchar(260) NULL,
        [SecretsFolderPathReal] nvarchar(260) NULL,
        [ProductionServerId] uniqueidentifier NULL,
        [ProductionEnvironmentId] uniqueidentifier NULL,
        [DeveloperDbConnectionId] uniqueidentifier NULL,
        [DatabaseExchangeFileStorageId] uniqueidentifier NULL,
        [UseSmartSchemaId] uniqueidentifier NULL,
        [Version] int NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ProjectCreatorSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProjectCreatorSettings_Singleton] CHECK ([Id] = '00000000-0000-0000-0000-000000000001'),
        CONSTRAINT [FK_ProjectCreatorSettings_DatabaseServerConnections_DeveloperDbConnectionId] FOREIGN KEY ([DeveloperDbConnectionId]) REFERENCES [DatabaseServerConnections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectCreatorSettings_Environments_ProductionEnvironmentId] FOREIGN KEY ([ProductionEnvironmentId]) REFERENCES [Environments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectCreatorSettings_FileStorages_DatabaseExchangeFileStorageId] FOREIGN KEY ([DatabaseExchangeFileStorageId]) REFERENCES [FileStorages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectCreatorSettings_Servers_ProductionServerId] FOREIGN KEY ([ProductionServerId]) REFERENCES [Servers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectCreatorSettings_SmartSchemas_UseSmartSchemaId] FOREIGN KEY ([UseSmartSchemaId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ProjectAllowedTools] (
        [Id] uniqueidentifier NOT NULL,
        [ToolName] nvarchar(50) NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ProjectAllowedTools] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectAllowedTools_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ProjectEndpoints] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [EndpointName] nvarchar(100) NULL,
        [EndpointRoute] nvarchar(256) NULL,
        [RequireAuthorization] bit NOT NULL,
        [HttpMethod] nvarchar(50) NOT NULL,
        [EndpointType] nvarchar(50) NOT NULL,
        [ReturnType] nvarchar(256) NULL,
        [SendMessageToCurrentUser] bit NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ProjectEndpoints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectEndpoints_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ProjectGitRepos] (
        [Id] uniqueidentifier NOT NULL,
        [GitRepoId] uniqueidentifier NOT NULL,
        [Kind] nvarchar(20) NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ProjectGitRepos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectGitRepos_GitRepos_GitRepoId] FOREIGN KEY ([GitRepoId]) REFERENCES [GitRepos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectGitRepos_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ProjectNpmPackages] (
        [Id] uniqueidentifier NOT NULL,
        [NpmPackageId] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ProjectNpmPackages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectNpmPackages_NpmPackages_NpmPackageId] FOREIGN KEY ([NpmPackageId]) REFERENCES [NpmPackages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectNpmPackages_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ProjectRedundantFiles] (
        [Id] uniqueidentifier NOT NULL,
        [FileName] nvarchar(260) NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ProjectRedundantFiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectRedundantFiles_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ProjectRouteClasses] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Root] nvarchar(50) NULL,
        [ApiVersion] nvarchar(50) NULL,
        [Base] nvarchar(256) NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ProjectRouteClasses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectRouteClasses_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ServerInfos] (
        [Id] uniqueidentifier NOT NULL,
        [ServerId] uniqueidentifier NOT NULL,
        [EnvironmentId] uniqueidentifier NOT NULL,
        [WebAgentForCheckId] uniqueidentifier NULL,
        [ServerSidePort] int NOT NULL,
        [ApiVersionId] nvarchar(50) NULL,
        [AppSettingsJsonSourceFileName] nvarchar(260) NULL,
        [AppSettingsEncodedJsonFileName] nvarchar(260) NULL,
        [ServiceUserName] nvarchar(128) NULL,
        [CurrentDatabaseParameters_DbConnectionId] uniqueidentifier NULL,
        [CurrentDatabaseParameters_DatabaseRecoveryModel] nvarchar(50) NULL,
        [CurrentDatabaseParameters_DbServerFoldersSetName] nvarchar(50) NULL,
        [CurrentDatabaseParameters_DatabaseName] nvarchar(128) NULL,
        [CurrentDatabaseParameters_SmartSchemaId] uniqueidentifier NULL,
        [CurrentDatabaseParameters_FileStorageId] uniqueidentifier NULL,
        [CurrentDatabaseParameters_CommandTimeOut] int NULL,
        [CurrentDatabaseParameters_SkipBackupBeforeRestore] bit NULL,
        [CurrentDatabaseParameters_BackupNamePrefix] nvarchar(100) NULL,
        [CurrentDatabaseParameters_DateMask] nvarchar(50) NULL,
        [CurrentDatabaseParameters_BackupFileExtension] nvarchar(50) NULL,
        [CurrentDatabaseParameters_BackupNameMiddlePart] nvarchar(100) NULL,
        [CurrentDatabaseParameters_Compress] bit NULL,
        [CurrentDatabaseParameters_Verify] bit NULL,
        [CurrentDatabaseParameters_BackupType] nvarchar(50) NULL,
        [NewDatabaseParameters_DbConnectionId] uniqueidentifier NULL,
        [NewDatabaseParameters_DatabaseRecoveryModel] nvarchar(50) NULL,
        [NewDatabaseParameters_DbServerFoldersSetName] nvarchar(50) NULL,
        [NewDatabaseParameters_DatabaseName] nvarchar(128) NULL,
        [NewDatabaseParameters_SmartSchemaId] uniqueidentifier NULL,
        [NewDatabaseParameters_FileStorageId] uniqueidentifier NULL,
        [NewDatabaseParameters_CommandTimeOut] int NULL,
        [NewDatabaseParameters_SkipBackupBeforeRestore] bit NULL,
        [NewDatabaseParameters_BackupNamePrefix] nvarchar(100) NULL,
        [NewDatabaseParameters_DateMask] nvarchar(50) NULL,
        [NewDatabaseParameters_BackupFileExtension] nvarchar(50) NULL,
        [NewDatabaseParameters_BackupNameMiddlePart] nvarchar(100) NULL,
        [NewDatabaseParameters_Compress] bit NULL,
        [NewDatabaseParameters_Verify] bit NULL,
        [NewDatabaseParameters_BackupType] nvarchar(50) NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ServerInfos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServerInfos_ApiClients_WebAgentForCheckId] FOREIGN KEY ([WebAgentForCheckId]) REFERENCES [ApiClients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServerInfos_DatabaseServerConnections_CurrentDatabaseParameters_DbConnectionId] FOREIGN KEY ([CurrentDatabaseParameters_DbConnectionId]) REFERENCES [DatabaseServerConnections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServerInfos_DatabaseServerConnections_NewDatabaseParameters_DbConnectionId] FOREIGN KEY ([NewDatabaseParameters_DbConnectionId]) REFERENCES [DatabaseServerConnections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServerInfos_Environments_EnvironmentId] FOREIGN KEY ([EnvironmentId]) REFERENCES [Environments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServerInfos_FileStorages_CurrentDatabaseParameters_FileStorageId] FOREIGN KEY ([CurrentDatabaseParameters_FileStorageId]) REFERENCES [FileStorages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServerInfos_FileStorages_NewDatabaseParameters_FileStorageId] FOREIGN KEY ([NewDatabaseParameters_FileStorageId]) REFERENCES [FileStorages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServerInfos_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ServerInfos_Servers_ServerId] FOREIGN KEY ([ServerId]) REFERENCES [Servers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServerInfos_SmartSchemas_CurrentDatabaseParameters_SmartSchemaId] FOREIGN KEY ([CurrentDatabaseParameters_SmartSchemaId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServerInfos_SmartSchemas_NewDatabaseParameters_SmartSchemaId] FOREIGN KEY ([NewDatabaseParameters_SmartSchemaId]) REFERENCES [SmartSchemas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE TABLE [ServerInfoAllowedTools] (
        [Id] uniqueidentifier NOT NULL,
        [ToolName] nvarchar(50) NOT NULL,
        [ServerInfoId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ServerInfoAllowedTools] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServerInfoAllowedTools_ServerInfos_ServerInfoId] FOREIGN KEY ([ServerInfoId]) REFERENCES [ServerInfos] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ApiClients_Name] ON [ApiClients] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DatabaseFoldersSets_DatabaseServerConnectionId_Name] ON [DatabaseFoldersSets] ([DatabaseServerConnectionId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_DatabaseServerConnections_DbWebAgentId] ON [DatabaseServerConnections] ([DbWebAgentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DatabaseServerConnections_Name] ON [DatabaseServerConnections] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DotnetTools_Name] ON [DotnetTools] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EditorConfigFileTypes_Name] ON [EditorConfigFileTypes] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Environments_Name] ON [Environments] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FileStorages_Name] ON [FileStorages] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GitIgnoreFileTypes_Name] ON [GitIgnoreFileTypes] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GitRepos_Address] ON [GitRepos] ([Address]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_GitRepos_GitIgnoreFileTypeId] ON [GitRepos] ([GitIgnoreFileTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GitRepos_Name] ON [GitRepos] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_GlobalSettings_DatabasesBackupFilesExchange_ExchangeFileStorageId] ON [GlobalSettings] ([DatabasesBackupFilesExchange_ExchangeFileStorageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_GlobalSettings_DatabasesBackupFilesExchange_ExchangeSmartSchemaId] ON [GlobalSettings] ([DatabasesBackupFilesExchange_ExchangeSmartSchemaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_GlobalSettings_DatabasesBackupFilesExchange_LocalSmartSchemaId] ON [GlobalSettings] ([DatabasesBackupFilesExchange_LocalSmartSchemaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_GlobalSettings_FileStorageForExchangeId] ON [GlobalSettings] ([FileStorageForExchangeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_GlobalSettings_LocalPackageManagerWebApiClientId] ON [GlobalSettings] ([LocalPackageManagerWebApiClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_GlobalSettings_SmartSchemaForExchangeId] ON [GlobalSettings] ([SmartSchemaForExchangeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_GlobalSettings_SmartSchemaForLocalId] ON [GlobalSettings] ([SmartSchemaForLocalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NpmPackages_Name] ON [NpmPackages] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectAllowedTools_ProjectId_ToolName] ON [ProjectAllowedTools] ([ProjectId], [ToolName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ProjectCreatorSettings_DatabaseExchangeFileStorageId] ON [ProjectCreatorSettings] ([DatabaseExchangeFileStorageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ProjectCreatorSettings_DeveloperDbConnectionId] ON [ProjectCreatorSettings] ([DeveloperDbConnectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ProjectCreatorSettings_ProductionEnvironmentId] ON [ProjectCreatorSettings] ([ProductionEnvironmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ProjectCreatorSettings_ProductionServerId] ON [ProjectCreatorSettings] ([ProductionServerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ProjectCreatorSettings_UseSmartSchemaId] ON [ProjectCreatorSettings] ([UseSmartSchemaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectEndpoints_ProjectId_Name] ON [ProjectEndpoints] ([ProjectId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ProjectGitRepos_GitRepoId] ON [ProjectGitRepos] ([GitRepoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectGitRepos_ProjectId_GitRepoId_Kind] ON [ProjectGitRepos] ([ProjectId], [GitRepoId], [Kind]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ProjectNpmPackages_NpmPackageId] ON [ProjectNpmPackages] ([NpmPackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectNpmPackages_ProjectId_NpmPackageId] ON [ProjectNpmPackages] ([ProjectId], [NpmPackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectRedundantFiles_ProjectId_FileName] ON [ProjectRedundantFiles] ([ProjectId], [FileName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectRouteClasses_ProjectId_Name] ON [ProjectRouteClasses] ([ProjectId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Projects_DevDatabaseParameters_DbConnectionId] ON [Projects] ([DevDatabaseParameters_DbConnectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Projects_DevDatabaseParameters_FileStorageId] ON [Projects] ([DevDatabaseParameters_FileStorageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Projects_DevDatabaseParameters_SmartSchemaId] ON [Projects] ([DevDatabaseParameters_SmartSchemaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Projects_EditorConfigFileTypeId] ON [Projects] ([EditorConfigFileTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Projects_Name] ON [Projects] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Projects_ProdCopyDatabaseParameters_DbConnectionId] ON [Projects] ([ProdCopyDatabaseParameters_DbConnectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Projects_ProdCopyDatabaseParameters_FileStorageId] ON [Projects] ([ProdCopyDatabaseParameters_FileStorageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Projects_ProdCopyDatabaseParameters_SmartSchemaId] ON [Projects] ([ProdCopyDatabaseParameters_SmartSchemaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectTemplates_Name] ON [ProjectTemplates] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ProjectTemplates_ReactTemplateId] ON [ProjectTemplates] ([ReactTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReactAppTemplates_Name] ON [ReactAppTemplates] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Runtimes_Name] ON [Runtimes] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServerInfoAllowedTools_ServerInfoId_ToolName] ON [ServerInfoAllowedTools] ([ServerInfoId], [ToolName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ServerInfos_CurrentDatabaseParameters_DbConnectionId] ON [ServerInfos] ([CurrentDatabaseParameters_DbConnectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ServerInfos_CurrentDatabaseParameters_FileStorageId] ON [ServerInfos] ([CurrentDatabaseParameters_FileStorageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ServerInfos_CurrentDatabaseParameters_SmartSchemaId] ON [ServerInfos] ([CurrentDatabaseParameters_SmartSchemaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ServerInfos_EnvironmentId] ON [ServerInfos] ([EnvironmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ServerInfos_NewDatabaseParameters_DbConnectionId] ON [ServerInfos] ([NewDatabaseParameters_DbConnectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ServerInfos_NewDatabaseParameters_FileStorageId] ON [ServerInfos] ([NewDatabaseParameters_FileStorageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ServerInfos_NewDatabaseParameters_SmartSchemaId] ON [ServerInfos] ([NewDatabaseParameters_SmartSchemaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServerInfos_ProjectId_ServerId_EnvironmentId] ON [ServerInfos] ([ProjectId], [ServerId], [EnvironmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ServerInfos_ServerId] ON [ServerInfos] ([ServerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_ServerInfos_WebAgentForCheckId] ON [ServerInfos] ([WebAgentForCheckId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Servers_Name] ON [Servers] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Servers_RuntimeId] ON [Servers] ([RuntimeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Servers_WebAgentId] ON [Servers] ([WebAgentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE INDEX [IX_Servers_WebAgentInstallerId] ON [Servers] ([WebAgentInstallerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SmartSchemaDetails_SmartSchemaId_PeriodType] ON [SmartSchemaDetails] ([SmartSchemaId], [PeriodType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SmartSchemas_Name] ON [SmartSchemas] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StoredFiles_Path] ON [StoredFiles] ([Path]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006113002_Initial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006113002_Initial', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007131749_AddGitRepoProjects'
)
BEGIN
    CREATE TABLE [GitRepoProjects] (
        [Id] uniqueidentifier NOT NULL,
        [GitRepoId] uniqueidentifier NOT NULL,
        [ProjectRelativePath] nvarchar(260) NOT NULL,
        [ProjectFileName] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_GitRepoProjects] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GitRepoProjects_GitRepos_GitRepoId] FOREIGN KEY ([GitRepoId]) REFERENCES [GitRepos] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007131749_AddGitRepoProjects'
)
BEGIN
    CREATE TABLE [GitRepoProjectDependencies] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectName] nvarchar(128) NOT NULL,
        [GitRepoProjectId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_GitRepoProjectDependencies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GitRepoProjectDependencies_GitRepoProjects_GitRepoProjectId] FOREIGN KEY ([GitRepoProjectId]) REFERENCES [GitRepoProjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007131749_AddGitRepoProjects'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GitRepoProjectDependencies_GitRepoProjectId_ProjectName] ON [GitRepoProjectDependencies] ([GitRepoProjectId], [ProjectName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007131749_AddGitRepoProjects'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GitRepoProjects_GitRepoId_ProjectRelativePath_ProjectFileName] ON [GitRepoProjects] ([GitRepoId], [ProjectRelativePath], [ProjectFileName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007131749_AddGitRepoProjects'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007131749_AddGitRepoProjects', N'10.0.12');
END;

COMMIT;
GO

