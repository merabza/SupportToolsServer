using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Serilog;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.DependencyInjection;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.DependencyInjection;

public sealed class SupportToolsServerApiDependencyInjectionTests
{
    [Fact]
    public async Task UseSupportToolsServerApi_MapsEveryEndpointGroup()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseSupportToolsServerApi(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/apiclients/delete/{key}", "DELETE api/v1/databaseserverconnections/delete/{key}",
            "DELETE api/v1/dotnettools/delete/{key}", "DELETE api/v1/environments/delete/{key}",
            "DELETE api/v1/files/delete", "DELETE api/v1/filestorages/delete/{key}",
            "DELETE api/v1/git/deleteeditorconfigfiletype/{key}",
            "DELETE api/v1/git/deletegitignorefiletype/{key}", "DELETE api/v1/git/deletegitrepo/{key}",
            "DELETE api/v1/npmpackages/delete/{key}", "DELETE api/v1/projects/delete/{key}",
            "DELETE api/v1/projecttemplates/delete/{key}",
            "DELETE api/v1/reactapptemplates/delete/{key}", "DELETE api/v1/runtimes/delete/{key}",
            "DELETE api/v1/servers/delete/{key}", "DELETE api/v1/smartschemas/delete/{key}",
            "GET api/v1/apiclients", "GET api/v1/apiclients/{key}", "GET api/v1/databaseserverconnections",
            "GET api/v1/databaseserverconnections/{key}", "GET api/v1/dotnettools", "GET api/v1/dotnettools/{key}",
            "GET api/v1/environments", "GET api/v1/environments/{key}", "GET api/v1/files",
            "GET api/v1/files/content", "GET api/v1/filestorages",
            "GET api/v1/filestorages/{key}", "GET api/v1/git/editorconfigfiletypeslist",
            "GET api/v1/git/gitignorefiletypeslist", "GET api/v1/git/gitrepo/{key}", "GET api/v1/git/gitrepos",
            "GET api/v1/npmpackages", "GET api/v1/npmpackages/{key}", "GET api/v1/projects",
            "GET api/v1/projects/{key}", "GET api/v1/projecttemplates",
            "GET api/v1/projecttemplates/{key}", "GET api/v1/reactapptemplates", "GET api/v1/reactapptemplates/{key}",
            "GET api/v1/runtimes", "GET api/v1/runtimes/{key}", "GET api/v1/servers", "GET api/v1/servers/{key}",
            "GET api/v1/settings/global", "GET api/v1/settings/projectcreator", "GET api/v1/smartschemas",
            "GET api/v1/smartschemas/{key}", "POST api/v1/apiclients/update/{key}",
            "POST api/v1/databaseserverconnections/update/{key}", "POST api/v1/dotnettools/update/{key}",
            "POST api/v1/environments/update/{key}", "POST api/v1/files/update",
            "POST api/v1/filestorages/update/{key}",
            "POST api/v1/git/syncupeditorconfigfiletypes/{merge?}",
            "POST api/v1/git/syncupgitignorefiletypes/{merge?}", "POST api/v1/git/updategitignorefiletype/{key}",
            "POST api/v1/git/updategitrepo/{key}", "POST api/v1/git/uploadgitrepos",
            "POST api/v1/npmpackages/update/{key}", "POST api/v1/projects/update/{key}",
            "POST api/v1/projecttemplates/update/{key}",
            "POST api/v1/reactapptemplates/update/{key}", "POST api/v1/runtimes/update/{key}",
            "POST api/v1/servers/update/{key}", "POST api/v1/settings/global/update",
            "POST api/v1/settings/projectcreator/update", "POST api/v1/smartschemas/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseSupportToolsServerApi_LogsTheStartAndTheEndAndPassesTheLoggerToTheGroups()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseSupportToolsServerApi(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseSupportToolsServerApi"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseSupportToolsServerApi"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseGitIgnoreFileTypesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseGitReposEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseEditorConfigFileTypesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseEnvironmentsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseRuntimesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseNpmPackagesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseReactAppTemplatesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseDotnetToolsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseSmartSchemasEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseFileStoragesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseApiClientsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseDatabaseServerConnectionsEndpoints"),
            Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseServersEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseGlobalSettingsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseProjectCreatorSettingsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseProjectTemplatesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseProjectsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Started", "UseStoredFilesEndpoints"), Times.Once);
    }
}
