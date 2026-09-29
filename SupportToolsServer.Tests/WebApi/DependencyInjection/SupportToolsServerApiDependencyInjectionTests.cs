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
    public async Task UseSupportToolsServerApi_MapsBothEndpointGroups()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseSupportToolsServerApi(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/git/deletegitignorefiletype/{key}", "DELETE api/v1/git/deletegitrepo/{key}",
            "GET api/v1/git/gitignorefiletypeslist", "GET api/v1/git/gitrepo/{key}", "GET api/v1/git/gitrepos",
            "POST api/v1/git/syncupgitignorefiletypes/{merge?}", "POST api/v1/git/updategitignorefiletype/{key}",
            "POST api/v1/git/updategitrepo/{key}", "POST api/v1/git/uploadgitrepos"
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
    }
}
