using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.Projects;
using SupportToolsServer.Application.Projects.GetProjectByName;
using SupportToolsServer.Application.Projects.GetProjects;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Projects;

public sealed class ProjectQueryHandlersTests
{
    private readonly Mock<IApiClientRepository> _apiClients = new();
    private readonly Server _bee = TestData.NewServer("bee");
    private readonly DatabaseServerConnection _connection = TestData.NewDatabaseServerConnection("Pc1.Sql");
    private readonly Mock<IDatabaseServerConnectionRepository> _connections = new();
    private readonly Server _dl360 = TestData.NewServer("dl360");
    private readonly EditorConfigFileType _editorConfig = TestData.NewEditorConfigFileType("default");
    private readonly Mock<IEditorConfigFileTypeRepository> _editorConfigs = new();
    private readonly Mock<IDeploymentEnvironmentRepository> _environments = new();
    private readonly FileStorage _fileStorage = TestData.NewFileStorage("Backups");
    private readonly Mock<IFileStorageRepository> _fileStorages = new();
    private readonly Mock<IGitRepoRepository> _gitRepos = new();
    private readonly Mock<INpmPackageRepository> _npmPackages = new();
    private readonly Server _pazisi = TestData.NewServer("PAZISI");
    private readonly DeploymentEnvironment _prod = TestData.NewEnvironment("Prod");
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly NpmPackage _react = TestData.NewNpmPackage("react");
    private readonly GitRepo _repoA;
    private readonly GitRepo _repoB;
    private readonly GitRepo _seeder;
    private readonly Mock<IServerRepository> _servers = new();
    private readonly SmartSchema _smartSchema = TestData.NewSmartSchema("Reduce");
    private readonly Mock<ISmartSchemaRepository> _smartSchemas = new();
    private readonly DeploymentEnvironment _test = TestData.NewEnvironment("Test");
    private readonly ApiClient _webAgent = TestData.NewApiClient("PAZISI.WebAgent");
    private readonly NpmPackage _yup = TestData.NewNpmPackage("Yup");

    public ProjectQueryHandlersTests()
    {
        var gitIgnoreFileType = TestData.NewGitIgnoreFileType("CSharp");
        _repoA = TestData.NewGitRepo("repoA", gitIgnoreFileType);
        _repoB = TestData.NewGitRepo("RepoB", gitIgnoreFileType);
        _seeder = TestData.NewGitRepo("Seeder", gitIgnoreFileType);
        _editorConfigs.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewEditorConfigFileType("strict"), _editorConfig]);
        _connections.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_connection, TestData.NewDatabaseServerConnection("Pc2.Sql")]);
        _smartSchemas.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_smartSchema]);
        _fileStorages.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_fileStorage]);
        _gitRepos.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_repoB, _seeder, _repoA]);
        _npmPackages.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_yup, _react]);
        _servers.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_pazisi, _dl360, _bee]);
        _environments.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_test, _prod]);
        _apiClients.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([_webAgent]);
    }

    private ProjectReferenceNames Names()
    {
        return new ProjectReferenceNames
        {
            EditorConfigFileTypes = new Dictionary<EditorConfigFileTypeId, string> { [_editorConfig.Id] = "default" },
            DatabaseServerConnections =
                new Dictionary<DatabaseServerConnectionId, string> { [_connection.Id] = "Pc1.Sql" },
            SmartSchemas = new Dictionary<SmartSchemaId, string> { [_smartSchema.Id] = "Reduce" },
            FileStorages = new Dictionary<FileStorageId, string> { [_fileStorage.Id] = "Backups" },
            GitRepos = new Dictionary<GitRepoId, string>
            {
                [_repoA.Id] = "repoA", [_repoB.Id] = "RepoB", [_seeder.Id] = "Seeder"
            },
            NpmPackages = new Dictionary<NpmPackageId, string> { [_react.Id] = "react", [_yup.Id] = "Yup" },
            Servers = new Dictionary<ServerId, string>
            {
                [_pazisi.Id] = "PAZISI", [_dl360.Id] = "dl360", [_bee.Id] = "bee"
            },
            Environments = new Dictionary<DeploymentEnvironmentId, string> { [_prod.Id] = "Prod", [_test.Id] = "Test" },
            ApiClients = new Dictionary<ApiClientId, string> { [_webAgent.Id] = "PAZISI.WebAgent" }
        };
    }

    private Task<Result<StsProjectDataModel>> GetByName(string name)
    {
        return new GetProjectByNameQueryHandler(_projects.Object, _editorConfigs.Object, _connections.Object,
            _smartSchemas.Object, _fileStorages.Object, _gitRepos.Object, _npmPackages.Object, _servers.Object,
            _environments.Object, _apiClients.Object).Handle(new GetProjectByNameQuery(name), CancellationToken.None);
    }

    //The order ignores case, so it differs from the ordinal one; the references are named, not given by their ids
    [Fact]
    public async Task GetProjects_ReturnsEveryProjectWithTheNamesOfItsReferencesInNameOrder()
    {
        _projects.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewProject("AppB", version: 2),
            TestData.NewProject("appA", _editorConfig, TestData.NewDatabaseParameters(_connection), null, [_repoA],
                [_seeder], [_react], [TestData.NewServerInfo(_pazisi, _prod, _webAgent)], 4),
            TestData.NewProject("AppC")
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsProjectDataModel>> result =
            await new GetProjectsQueryHandler(_projects.Object, _editorConfigs.Object, _connections.Object,
                _smartSchemas.Object, _fileStorages.Object, _gitRepos.Object, _npmPackages.Object, _servers.Object,
                _environments.Object, _apiClients.Object).Handle(new GetProjectsQuery(), cancellation.Token);

        Assert.Equal(["appA", "AppB", "AppC"], result.Value.Select(x => x.Name));
        Assert.Equal(["default", null, null], result.Value.Select(x => x.EditorConfigPatternName));
        Assert.Equal("Pc1.Sql", result.Value[0].DevDatabaseParameters!.DbConnectionName);
        Assert.Equal(["repoA"], result.Value[0].GitProjectNames);
        Assert.Equal(["Seeder"], result.Value[0].ScaffoldSeederGitProjectNames);
        Assert.Equal(["react"], result.Value[0].FrontNpmPackageNames);
        StsServerInfoDataModel serverInfo = Assert.Single(result.Value[0].ServerInfos);
        Assert.Equal("PAZISI", serverInfo.ServerName);
        Assert.Equal("Prod", serverInfo.EnvironmentName);
        Assert.Equal("PAZISI.WebAgent", serverInfo.WebAgentNameForCheck);
        Assert.Empty(result.Value[1].ServerInfos);
        Assert.Equal([4, 2, 1], result.Value.Select(x => x.Version));
        _projects.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _editorConfigs.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _connections.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _smartSchemas.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _fileStorages.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _gitRepos.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _npmPackages.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _servers.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _environments.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _apiClients.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetProjectByName_ReturnsTheProjectWithItsReferenceNamesAndVersion()
    {
        _projects.Setup(r => r.GetByName("APPA", It.IsAny<CancellationToken>())).ReturnsAsync(
            TestData.NewProject("AppA", _editorConfig, null,
                TestData.NewDatabaseParameters(_connection, _smartSchema, _fileStorage), [_repoB], [], [_yup],
                [TestData.NewServerInfo(_dl360, _test, null, null, TestData.NewDatabaseParameters(_connection))], 5));

        Result<StsProjectDataModel> result = await GetByName("APPA");

        Assert.Equal("AppA", result.Value.Name);
        Assert.Equal("default", result.Value.EditorConfigPatternName);
        Assert.Null(result.Value.DevDatabaseParameters);
        Assert.Equal("Pc1.Sql", result.Value.ProdCopyDatabaseParameters!.DbConnectionName);
        Assert.Equal("Reduce", result.Value.ProdCopyDatabaseParameters.SmartSchemaName);
        Assert.Equal("Backups", result.Value.ProdCopyDatabaseParameters.FileStorageName);
        Assert.Equal(["RepoB"], result.Value.GitProjectNames);
        Assert.Equal(["Yup"], result.Value.FrontNpmPackageNames);
        StsServerInfoDataModel serverInfo = Assert.Single(result.Value.ServerInfos);
        Assert.Equal("dl360|Test", $"{serverInfo.ServerName}|{serverInfo.EnvironmentName}");
        Assert.Null(serverInfo.WebAgentNameForCheck);
        Assert.Null(serverInfo.CurrentDatabaseParameters);
        Assert.Equal("Pc1.Sql", serverInfo.NewDatabaseParameters!.DbConnectionName);
        Assert.Equal(TestData.MadeUpKeyGuidPart, result.Value.KeyGuidPart);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetProjectByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _projects.Setup(r => r.GetByName("AppZ", It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);

        Result<StsProjectDataModel> result = await GetByName("AppZ");

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Project With Name AppZ Not Found", result.Error.Description);
        _gitRepos.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
        _servers.Verify(r => r.GetAll(It.IsAny<CancellationToken>()), Times.Never);
    }

    //The values of TestData.NewProject come back as the values of TestData.ProjectModel
    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        Project project = TestData.NewProject("AppA", _editorConfig, TestData.NewDatabaseParameters(_connection),
            TestData.NewDatabaseParameters(_connection, _smartSchema, _fileStorage, "AppAProdCopy"), [_repoA],
            [_seeder], [_react],
            [
                TestData.NewServerInfo(_pazisi, _prod, _webAgent,
                    TestData.NewDatabaseParameters(_connection, _smartSchema, _fileStorage, "AppA"),
                    serverSidePort: 5050),
                TestData.NewServerInfo(_pazisi, _test, null, null, TestData.NewDatabaseParameters(databaseName: "New"))
            ], 7);
        StsProjectDataModel expected = TestData.ProjectModel("AppA", "default",
            TestData.DatabaseParametersModel("Pc1.Sql"),
            TestData.DatabaseParametersModel("Pc1.Sql", "Reduce", "Backups", "AppAProdCopy"), ["repoA"], ["Seeder"],
            ["react"],
            [
                TestData.ServerInfoModel("PAZISI", "Prod", "PAZISI.WebAgent",
                    TestData.DatabaseParametersModel("Pc1.Sql", "Reduce", "Backups", "AppA"), serverSidePort: 5050),
                TestData.ServerInfoModel("PAZISI", "Test", null, null,
                    TestData.DatabaseParametersModel(databaseName: "New"))
            ], 7);

        StsProjectDataModel model = project.ToContractModel(Names());

        Assert.Equal(JsonSerializer.Serialize(expected),
            JsonSerializer.Serialize(model));
    }

    //The lists of the client are sets or dictionaries, so the contract sorts them, ignoring case, for a stable hash on
    //the client: the gits of each role, the packages, the files and the tools by name, the endpoints and the route
    //classes by their key, the server infos by the server and the environment and their tools by name
    [Fact]
    public void ToContractModel_SortsEveryListByNameIgnoringCase()
    {
        Project project = TestData.NewProject("AppA", gitRepos: [_repoB, _seeder, _repoA],
            scaffoldSeederGitRepos: [_seeder, _repoA], npmPackages: [_yup, _react]);
        project.Update(project.Name, project.ProjectType, null, null, 1, 0, false, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, project.GitRepos, project.NpmPackages,
            [ProjectRedundantFile.Create("web.config"), ProjectRedundantFile.Create("*.PDB")],
            [ProjectAllowedTool.Create("SeedData"), ProjectAllowedTool.Create("CorrectNewDatabase")],
            [
                ProjectEndpoint.Create("upload", null, null, false, "Post", "Command", null, false),
                ProjectEndpoint.Create("Get", null, null, false, "Get", "Query", null, false)
            ], [ProjectRouteClass.Create("main", null, null, null), ProjectRouteClass.Create("Git", null, null, null)],
            [
                ServerInfo.Create(_pazisi.Id, _test.Id, null, 0, null, null, null, null, null, null,
                    [
                        ServerInfoAllowedTool.Create("VersionChecker"), ServerInfoAllowedTool.Create("programUpdater"),
                        ServerInfoAllowedTool.Create("AppSettingsEncoder")
                    ]),
                ServerInfo.Create(_dl360.Id, _prod.Id, null, 0, null, null, null, null, null, null, []),
                ServerInfo.Create(_pazisi.Id, _prod.Id, null, 0, null, null, null, null, null, null, []),
                ServerInfo.Create(_bee.Id, _test.Id, null, 0, null, null, null, null, null, null, [])
            ]);

        StsProjectDataModel model = project.ToContractModel(Names());

        Assert.Equal(["repoA", "RepoB", "Seeder"], model.GitProjectNames);
        Assert.Equal(["repoA", "Seeder"], model.ScaffoldSeederGitProjectNames);
        Assert.Equal(["react", "Yup"], model.FrontNpmPackageNames);
        Assert.Equal(["*.PDB", "web.config"], model.RedundantFileNames);
        Assert.Equal(["CorrectNewDatabase", "SeedData"], model.AllowToolsList);
        Assert.Equal(["Get", "upload"], model.Endpoints.Select(x => x.Name));
        Assert.Equal(["Git", "main"], model.RouteClasses.Select(x => x.Name));
        Assert.Equal(["bee|Test", "dl360|Prod", "PAZISI|Prod", "PAZISI|Test"],
            model.ServerInfos.Select(x => $"{x.ServerName}|{x.EnvironmentName}"));
        Assert.Equal(["AppSettingsEncoder", "programUpdater", "VersionChecker"], model.ServerInfos[3].AllowToolsList);
    }
}
