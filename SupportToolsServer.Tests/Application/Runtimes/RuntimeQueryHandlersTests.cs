using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.Runtimes;
using SupportToolsServer.Application.Runtimes.GetRuntimeByName;
using SupportToolsServer.Application.Runtimes.GetRuntimes;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Runtimes;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.Runtimes;

public sealed class RuntimeQueryHandlersTests
{
    private readonly Mock<IRuntimeRepository> _runtimes = new();

    //The order ignores case, so it differs from the ordinal one
    [Fact]
    public async Task GetRuntimes_ReturnsEveryFieldAndTheVersionInNameOrder()
    {
        _runtimes.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewRuntime("win-arm64", "Windows ARM64"), TestData.NewRuntime("android-arm64", null, 4),
            TestData.NewRuntime("Linux-musl-x64", "Alpine", 2)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsRuntimeDataModel>> result =
            await new GetRuntimesQueryHandler(_runtimes.Object).Handle(new GetRuntimesQuery(), cancellation.Token);

        Assert.Equal(["android-arm64", "Linux-musl-x64", "win-arm64"], result.Value.Select(x => x.Name));
        Assert.Equal([null, "Alpine", "Windows ARM64"], result.Value.Select(x => x.Description));
        Assert.Equal([4, 2, 1], result.Value.Select(x => x.Version));
        _runtimes.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetRuntimeByName_ReturnsTheRecordWithItsVersion()
    {
        _runtimes.Setup(r => r.GetByName("WIN-X64", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewRuntime("win-x64", "Windows x64", 5));

        Result<StsRuntimeDataModel> result =
            await new GetRuntimeByNameQueryHandler(_runtimes.Object).Handle(new GetRuntimeByNameQuery("WIN-X64"),
                CancellationToken.None);

        Assert.Equal("win-x64", result.Value.Name);
        Assert.Equal("Windows x64", result.Value.Description);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetRuntimeByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _runtimes.Setup(r => r.GetByName("osx-arm64", It.IsAny<CancellationToken>())).ReturnsAsync((Runtime?)null);

        Result<StsRuntimeDataModel> result =
            await new GetRuntimeByNameQueryHandler(_runtimes.Object).Handle(new GetRuntimeByNameQuery("osx-arm64"),
                CancellationToken.None);

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Runtime With Name osx-arm64 Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        StsRuntimeDataModel model = TestData.NewRuntime("win-x64", "Windows x64", 7).ToContractModel();

        Assert.Equal("win-x64", model.Name);
        Assert.Equal("Windows x64", model.Description);
        Assert.Equal(7, model.Version);
    }
}
