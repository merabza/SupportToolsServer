using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SupportToolsServer.Application.Projects.UpdateProject;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Projects;

public sealed class UpdateProjectCommandHandlerTests
{
    private readonly DatabaseServerConnection _connection = TestData.NewDatabaseServerConnection("Pc1.Sql");
    private readonly Mock<IDatabaseServerConnectionRepository> _connections = new();
    private readonly EditorConfigFileType _editorConfig = TestData.NewEditorConfigFileType("default");
    private readonly Mock<IEditorConfigFileTypeRepository> _editorConfigs = new();
    private readonly FileStorage _fileStorage = TestData.NewFileStorage("Backups");
    private readonly Mock<IFileStorageRepository> _fileStorages = new();
    private readonly GitRepo _repoA;
    private readonly GitRepo _repoB;
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly NpmPackage _react = TestData.NewNpmPackage("react");
    private readonly Mock<INpmPackageRepository> _npmPackages = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly SmartSchema _smartSchema = TestData.NewSmartSchema("Reduce");
    private readonly Mock<ISmartSchemaRepository> _smartSchemas = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateProjectCommandHandlerTests()
    {
        var gitIgnoreFileType = TestData.NewGitIgnoreFileType("CSharp");
        _repoA = TestData.NewGitRepo("RepoA", gitIgnoreFileType);
        _repoB = TestData.NewGitRepo("RepoB", gitIgnoreFileType);
        _editorConfigs.Setup(r => r.GetByName("default", It.IsAny<CancellationToken>())).ReturnsAsync(_editorConfig);
        _connections.Setup(r => r.GetByName("Pc1.Sql", It.IsAny<CancellationToken>())).ReturnsAsync(_connection);
        _smartSchemas.Setup(r => r.GetByName("Reduce", It.IsAny<CancellationToken>())).ReturnsAsync(_smartSchema);
        _fileStorages.Setup(r => r.GetByName("Backups", It.IsAny<CancellationToken>())).ReturnsAsync(_fileStorage);
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(() => [_repoA, _repoB]);
        _npmPackages.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [_react, TestData.NewNpmPackage("yup")]);
    }

    //The handler reads the project with tracking, so that the save deletes the replaced children
    private void GivenStored(string name, Project? stored)
    {
        _projects.Setup(r => r.GetByNameForUpdate(name, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
    }

    private Task<Result<int>> Handle(StsProjectDataModel model, CancellationToken cancellationToken = default)
    {
        var handler = new UpdateProjectCommandHandler(_projects.Object, _editorConfigs.Object, _connections.Object,
            _smartSchemas.Object, _fileStorages.Object, _gitRepos.Object, _npmPackages.Object, _unitOfWork.Object);
        return handler.Handle(new UpdateProjectCommand(model), cancellationToken);
    }

    private void VerifyNothingSaved()
    {
        _projects.Verify(r => r.Add(It.IsAny<Project>()), Times.Never);
        _projects.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    //Every text field of the body gets its own value, so that a swapped field fails
    private static StsProjectDataModel ModelWithEveryValue()
    {
        return new StsProjectDataModel
        {
            Name = "AppA",
            ProjectType = "IsPackage",
            ProjectGroupName = "Group",
            ProjectDescription = "Description",
            MajorVersion = 3,
            MinorVersion = 7,
            UseAlternativeWebAgent = true,
            EditorConfigPatternName = "default",
            MainProjectName = "Main",
            ApiContractsProjectName = "ApiContracts",
            SpaProjectName = "Spa",
            DbContextName = "DbContext",
            ProjectShortPrefix = "Prefix",
            ScaffoldSeederProjectName = "ScaffoldSeeder",
            DbContextProjectName = "DbContextProject",
            NewDataSeedingClassLibProjectName = "NewDataSeeding",
            ProgramArchiveDateMask = "ArchiveMask",
            ProgramArchiveExtension = "ArchiveExtension",
            ParametersFileDateMask = "ParametersMask",
            ParametersFileExtension = "ParametersExtension",
            ProjectFolderName = "Folder",
            SolutionFileName = "Solution",
            ProjectSecurityFolderPath = "Security",
            MigrationStartupProjectFilePath = "MigrationStartup",
            MigrationProjectFilePath = "Migration",
            DataSeederRulesByTableStartupProjectFilePath = "SeederRules",
            OldDataConvertorForDataSeeder = "OldDataConvertor",
            SeedProjectFilePath = "Seed",
            SeedProjectParametersFilePath = "SeedParameters",
            ExcludesRulesParametersFilePath = "ExcludesRules",
            AppSetEnKeysJsonFileName = "AppSetEnKeys",
            MigrationSqlFilesFolder = "MigrationSql",
            PrepareProdCopyDatabaseProjectFilePath = "PrepareProdCopy",
            PrepareProdCopyDatabaseProjectParametersFilePath = "PrepareProdCopyParameters",
            PairedDbObjectsResultFileName = "PairedDbObjects",
            KeyGuidPart = TestData.MadeUpKeyGuidPart,
            DevDatabaseParameters = new StsDatabaseParametersDataModel
            {
                DbConnectionName = "Pc1.Sql",
                DatabaseRecoveryModel = "Simple",
                DbServerFoldersSetName = "Second",
                DatabaseName = "AppADev",
                SmartSchemaName = "Reduce",
                FileStorageName = "Backups",
                CommandTimeOut = 90,
                SkipBackupBeforeRestore = true,
                BackupNamePrefix = "BackupPrefix",
                DateMask = "DateMask",
                BackupFileExtension = "BackupExtension",
                BackupNameMiddlePart = "MiddlePart",
                Compress = false,
                Verify = true,
                BackupType = "Diff"
            },
            GitProjectNames = ["repoa", "RepoB"],
            ScaffoldSeederGitProjectNames = ["RepoA"],
            FrontNpmPackageNames = ["REACT"],
            RedundantFileNames = ["*.pdb", "*.xml"],
            AllowToolsList = ["SeedData"],
            Endpoints =
            [
                new StsProjectEndpointDataModel
                {
                    Name = "Key",
                    EndpointName = "EndpointName",
                    EndpointRoute = "Route",
                    RequireAuthorization = true,
                    HttpMethod = "Patch",
                    EndpointType = "Query",
                    ReturnType = "ReturnType",
                    SendMessageToCurrentUser = true
                }
            ],
            RouteClasses =
            [
                new StsProjectRouteClassDataModel
                {
                    Name = "RouteKey", Root = "Root", ApiVersion = "ApiVersion", Base = "Base"
                }
            ]
        };
    }

    [Fact]
    public async Task Handle_CreatesTheProjectWithEveryValueTheReferencesAndTheChildrenAndTheFirstVersion()
    {
        GivenStored("AppA", null);
        Project? added = null;
        _projects.Setup(r => r.Add(It.IsAny<Project>())).Callback<Project>(x => added = x);
        using var cancellation = new CancellationTokenSource();

        Result<int> result = await Handle(ModelWithEveryValue(), cancellation.Token);

        Assert.Equal(1, result.Value);
        Assert.NotNull(added);
        Assert.Equal("AppA", added.Name);
        Assert.Equal("IsPackage", added.ProjectType);
        Assert.Equal("Group", added.ProjectGroupName);
        Assert.Equal("Description", added.ProjectDescription);
        Assert.Equal(3, added.MajorVersion);
        Assert.Equal(7, added.MinorVersion);
        Assert.True(added.UseAlternativeWebAgent);
        Assert.Equal(_editorConfig.Id, added.EditorConfigFileTypeId);
        Assert.Equal("Main", added.MainProjectName);
        Assert.Equal("ApiContracts", added.ApiContractsProjectName);
        Assert.Equal("Spa", added.SpaProjectName);
        Assert.Equal("DbContext", added.DbContextName);
        Assert.Equal("Prefix", added.ProjectShortPrefix);
        Assert.Equal("ScaffoldSeeder", added.ScaffoldSeederProjectName);
        Assert.Equal("DbContextProject", added.DbContextProjectName);
        Assert.Equal("NewDataSeeding", added.NewDataSeedingClassLibProjectName);
        Assert.Equal("ArchiveMask", added.ProgramArchiveDateMask);
        Assert.Equal("ArchiveExtension", added.ProgramArchiveExtension);
        Assert.Equal("ParametersMask", added.ParametersFileDateMask);
        Assert.Equal("ParametersExtension", added.ParametersFileExtension);
        Assert.Equal("Folder", added.ProjectFolderName);
        Assert.Equal("Solution", added.SolutionFileName);
        Assert.Equal("Security", added.ProjectSecurityFolderPath);
        Assert.Equal("MigrationStartup", added.MigrationStartupProjectFilePath);
        Assert.Equal("Migration", added.MigrationProjectFilePath);
        Assert.Equal("SeederRules", added.DataSeederRulesByTableStartupProjectFilePath);
        Assert.Equal("OldDataConvertor", added.OldDataConvertorForDataSeeder);
        Assert.Equal("Seed", added.SeedProjectFilePath);
        Assert.Equal("SeedParameters", added.SeedProjectParametersFilePath);
        Assert.Equal("ExcludesRules", added.ExcludesRulesParametersFilePath);
        Assert.Equal("AppSetEnKeys", added.AppSetEnKeysJsonFileName);
        Assert.Equal("MigrationSql", added.MigrationSqlFilesFolder);
        Assert.Equal("PrepareProdCopy", added.PrepareProdCopyDatabaseProjectFilePath);
        Assert.Equal("PrepareProdCopyParameters", added.PrepareProdCopyDatabaseProjectParametersFilePath);
        Assert.Equal("PairedDbObjects", added.PairedDbObjectsResultFileName);
        Assert.Equal(TestData.MadeUpKeyGuidPart, added.KeyGuidPart);
        DatabaseParameters dev = Assert.IsType<DatabaseParameters>(added.DevDatabaseParameters);
        Assert.Equal(_connection.Id, dev.DbConnectionId);
        Assert.Equal("Simple", dev.DatabaseRecoveryModel);
        Assert.Equal("Second", dev.DbServerFoldersSetName);
        Assert.Equal("AppADev", dev.DatabaseName);
        Assert.Equal(_smartSchema.Id, dev.SmartSchemaId);
        Assert.Equal(_fileStorage.Id, dev.FileStorageId);
        Assert.Equal(90, dev.CommandTimeOut);
        Assert.True(dev.SkipBackupBeforeRestore);
        Assert.Equal("BackupPrefix", dev.BackupNamePrefix);
        Assert.Equal("DateMask", dev.DateMask);
        Assert.Equal("BackupExtension", dev.BackupFileExtension);
        Assert.Equal("MiddlePart", dev.BackupNameMiddlePart);
        Assert.False(dev.Compress);
        Assert.True(dev.Verify);
        Assert.Equal("Diff", dev.BackupType);
        Assert.Null(added.ProdCopyDatabaseParameters);
        Assert.Equal(
            [
                (_repoA.Id, EProjectGitRepoKind.Main), (_repoB.Id, EProjectGitRepoKind.Main),
                (_repoA.Id, EProjectGitRepoKind.ScaffoldSeed)
            ], added.GitRepos.Select(x => (x.GitRepoId, x.Kind)));
        Assert.Equal([_react.Id], added.NpmPackages.Select(x => x.NpmPackageId));
        Assert.Equal(["*.pdb", "*.xml"], added.RedundantFiles.Select(x => x.FileName));
        Assert.Equal(["SeedData"], added.AllowedTools.Select(x => x.ToolName));
        ProjectEndpoint endpoint = Assert.Single(added.Endpoints);
        Assert.Equal(
            ("Key", "EndpointName", "Route", true, "Patch", "Query", "ReturnType", true),
            (endpoint.Name, endpoint.EndpointName, endpoint.EndpointRoute, endpoint.RequireAuthorization,
                endpoint.HttpMethod, endpoint.EndpointType, endpoint.ReturnType, endpoint.SendMessageToCurrentUser));
        ProjectRouteClass routeClass = Assert.Single(added.RouteClasses);
        Assert.Equal(("RouteKey", "Root", "ApiVersion", "Base"),
            (routeClass.Name, routeClass.Root, routeClass.ApiVersion, routeClass.Base));
        Assert.Equal(1, added.Version);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    //No name is no reference, and a project without gits or packages reads neither
    [Fact]
    public async Task Handle_ReadsNoReference_WhenTheBodyNamesNone()
    {
        GivenStored("AppA", null);
        StsProjectDataModel model = TestData.ProjectModel("AppA", " ",
            new StsDatabaseParametersDataModel { DbConnectionName = "", SmartSchemaName = null });

        Result<int> result = await Handle(model);

        Assert.Equal(1, result.Value);
        _editorConfigs.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _connections.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _smartSchemas.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _fileStorages.Verify(r => r.GetByName(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _gitRepos.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _npmPackages.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
    }

    //Every missing name is in one error, grouped by type in the order of the contract and each name once
    [Fact]
    public async Task Handle_ReturnsEveryMissingReferenceInOneError_WhenReferencedRecordsDoNotExist()
    {
        GivenStored("AppA", TestData.NewProject("AppA", version: 2));
        StsProjectDataModel model = TestData.ProjectModel("AppA", "strict",
            TestData.DatabaseParametersModel("Pc9.Sql", "Hourly", "Backups"),
            TestData.DatabaseParametersModel("Pc8.Sql", "Reduce", "Exchange"), ["RepoA", "RepoX"], ["RepoX", "RepoY"],
            ["react", "left-pad"], 2);

        Result<int> result = await Handle(model);

        Assert.Equal("ReferencedRecordsNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(
            "Referenced EditorConfigFileType Records Not Found: strict; " +
            "Referenced DatabaseServerConnection Records Not Found: Pc9.Sql, Pc8.Sql; " +
            "Referenced SmartSchema Records Not Found: Hourly; Referenced FileStorage Records Not Found: Exchange; " +
            "Referenced GitRepo Records Not Found: RepoX, RepoY; Referenced NpmPackage Records Not Found: left-pad",
            result.Error.Description);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenVersionZeroIsExpectedButTheNameExists()
    {
        GivenStored("AppA", TestData.NewProject("APPA", version: 2));

        Result<int> result = await Handle(TestData.ProjectModel("AppA"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Project AppA Version Conflict: Expected 0, Actual 2", result.Error.Description);
        VerifyNothingSaved();
        _gitRepos.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
    }

    //The update replaces the whole aggregate: the stored children and database parameters are gone, those of the body
    //are the only ones
    [Fact]
    public async Task Handle_ReplacesTheStoredProjectWithItsChildrenAndReturnsItsNextVersion()
    {
        Project stored = TestData.NewProject("AppA", _editorConfig, TestData.NewDatabaseParameters(_connection),
            null, [_repoA], [_repoB], [_react], 3);
        GivenStored("APPA", stored);
        StsProjectDataModel model = TestData.ProjectModel("APPA", null, null,
            TestData.DatabaseParametersModel("Pc1.Sql", "Reduce", null, "AppAProdCopy"), ["RepoB"], [], [], 3);
        model.RedundantFileNames = [];
        model.Endpoints = [];

        Result<int> result = await Handle(model);

        Assert.Equal(4, result.Value);
        _projects.Verify(r => r.Update(stored), Times.Once);
        Assert.Equal("APPA", stored.Name);
        Assert.Null(stored.EditorConfigFileTypeId);
        Assert.Null(stored.DevDatabaseParameters);
        DatabaseParameters prodCopy = Assert.IsType<DatabaseParameters>(stored.ProdCopyDatabaseParameters);
        Assert.Equal(_connection.Id, prodCopy.DbConnectionId);
        Assert.Equal(_smartSchema.Id, prodCopy.SmartSchemaId);
        Assert.Null(prodCopy.FileStorageId);
        Assert.Equal("AppAProdCopy", prodCopy.DatabaseName);
        Assert.Equal([(_repoB.Id, EProjectGitRepoKind.Main)], stored.GitRepos.Select(x => (x.GitRepoId, x.Kind)));
        Assert.Empty(stored.NpmPackages);
        Assert.Empty(stored.RedundantFiles);
        Assert.Equal(["SeedData"], stored.AllowedTools.Select(x => x.ToolName));
        Assert.Empty(stored.Endpoints);
        Assert.Equal(["Main"], stored.RouteClasses.Select(x => x.Name));
        Assert.Equal(4, stored.Version);
        _projects.Verify(r => r.Add(It.IsAny<Project>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Handle_ReturnsConcurrencyConflictWithTheActualVersion_WhenAnotherVersionIsStored(
        int expectedVersion)
    {
        GivenStored("AppA", TestData.NewProject("AppA", version: 3));

        Result<int> result = await Handle(TestData.ProjectModel("AppA", version: expectedVersion));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal($"Project AppA Version Conflict: Expected {expectedVersion}, Actual 3", result.Error.Description);
        VerifyNothingSaved();
    }

    //The project was deleted on the server after the client last saw it
    [Fact]
    public async Task Handle_ReturnsRecordWithNameNotFound_WhenAVersionIsExpectedButThereIsNoProject()
    {
        GivenStored("AppA", null);

        Result<int> result = await Handle(TestData.ProjectModel("AppA", version: 2));

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Project With Name AppA Not Found", result.Error.Description);
        VerifyNothingSaved();
    }

    //Another request saved version 2 between the read and the save. The version is read again without tracking, so it
    //is the stored one and not the version of the project this request changed
    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheProjectChangesBeforeTheSave()
    {
        GivenStored("AppA", TestData.NewProject("AppA"));
        _projects.Setup(r => r.GetByName("AppA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProject("AppA", version: 2));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        Result<int> result = await Handle(TestData.ProjectModel("AppA", version: 1));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Project AppA Version Conflict: Expected 1, Actual 2", result.Error.Description);
    }

    [Fact]
    public async Task Handle_ReturnsConcurrencyConflict_WhenTheSameNameIsCreatedBeforeTheSave()
    {
        GivenStored("AppA", null);
        _projects.Setup(r => r.GetByName("AppA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProject("AppA"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique index"));

        Result<int> result = await Handle(TestData.ProjectModel("AppA"));

        Assert.Equal("ConcurrencyConflict", result.Error.Code);
        Assert.Equal("Project AppA Version Conflict: Expected 0, Actual 1", result.Error.Description);
    }

    //A save that fails for another reason (here: a git was deleted after it was read) keeps its exception
    [Fact]
    public async Task Handle_ThrowsTheSaveException_WhenTheVersionStillMatches()
    {
        GivenStored("AppA", TestData.NewProject("AppA"));
        _projects.Setup(r => r.GetByName("AppA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProject("AppA"));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("foreign key"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Handle(TestData.ProjectModel("AppA", gitProjectNames: ["RepoA"], version: 1)));
    }
}
