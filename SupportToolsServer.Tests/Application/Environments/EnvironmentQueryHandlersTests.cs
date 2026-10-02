using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.Environments;
using SupportToolsServer.Application.Environments.GetEnvironmentByName;
using SupportToolsServer.Application.Environments.GetEnvironments;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Environments;

public sealed class EnvironmentQueryHandlersTests
{
    private readonly Mock<IDeploymentEnvironmentRepository> _environments = new();

    [Fact]
    public async Task GetEnvironments_ReturnsEveryFieldAndTheVersionInNameOrder()
    {
        _environments.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewEnvironment("Test", "Test"), TestData.NewEnvironment("dev", null, 4),
            TestData.NewEnvironment("Prod", "Production", 2)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsEnvironmentDataModel>> result =
            await new GetEnvironmentsQueryHandler(_environments.Object).Handle(new GetEnvironmentsQuery(),
                cancellation.Token);

        Assert.Equal(["dev", "Prod", "Test"], result.Value.Select(x => x.Name));
        Assert.Equal([null, "Production", "Test"], result.Value.Select(x => x.Description));
        Assert.Equal([4, 2, 1], result.Value.Select(x => x.Version));
        _environments.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetEnvironmentByName_ReturnsTheRecordWithItsVersion()
    {
        _environments.Setup(r => r.GetByName("prod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewEnvironment("Prod", "Production", 5));

        Result<StsEnvironmentDataModel> result =
            await new GetEnvironmentByNameQueryHandler(_environments.Object).Handle(
                new GetEnvironmentByNameQuery("prod"), CancellationToken.None);

        Assert.Equal("Prod", result.Value.Name);
        Assert.Equal("Production", result.Value.Description);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetEnvironmentByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _environments.Setup(r => r.GetByName("Stage", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeploymentEnvironment?)null);

        Result<StsEnvironmentDataModel> result =
            await new GetEnvironmentByNameQueryHandler(_environments.Object).Handle(
                new GetEnvironmentByNameQuery("Stage"), CancellationToken.None);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Environment With Name Stage Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        StsEnvironmentDataModel model = TestData.NewEnvironment("Prod", "Production", 7).ToContractModel();

        Assert.Equal("Prod", model.Name);
        Assert.Equal("Production", model.Description);
        Assert.Equal(7, model.Version);
    }
}
