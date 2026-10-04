using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.DotnetTools;
using SupportToolsServer.Application.DotnetTools.GetDotnetToolByName;
using SupportToolsServer.Application.DotnetTools.GetDotnetTools;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DotnetTools;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.DotnetTools;

public sealed class DotnetToolQueryHandlersTests
{
    private readonly Mock<IDotnetToolRepository> _dotnetTools = new();

    private Task<Result<StsDotnetToolDataModel>> GetByName(string name)
    {
        return new GetDotnetToolByNameQueryHandler(_dotnetTools.Object).Handle(new GetDotnetToolByNameQuery(name),
            CancellationToken.None);
    }

    //The order ignores case, so it differs from the ordinal one
    [Fact]
    public async Task GetDotnetTools_ReturnsEveryFieldAndTheVersionInNameOrder()
    {
        _dotnetTools.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewDotnetTool("ReportGenerator", "dotnet-reportgenerator-globaltool", null, "coverage"),
            TestData.NewDotnetTool("jb", "jetbrains.resharper.globaltools", "2026.2.3.1", null, 4),
            TestData.NewDotnetTool("DotnetEf", "dotnet-ef", "9.0.8", "Entity Framework", 2)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsDotnetToolDataModel>> result =
            await new GetDotnetToolsQueryHandler(_dotnetTools.Object).Handle(new GetDotnetToolsQuery(),
                cancellation.Token);

        Assert.Equal(["DotnetEf", "jb", "ReportGenerator"], result.Value.Select(x => x.Name));
        Assert.Equal(["dotnet-ef", "jetbrains.resharper.globaltools", "dotnet-reportgenerator-globaltool"],
            result.Value.Select(x => x.PackageId));
        Assert.Equal(["9.0.8", "2026.2.3.1", null], result.Value.Select(x => x.MaxVersion));
        Assert.Equal(["Entity Framework", null, "coverage"], result.Value.Select(x => x.Description));
        Assert.Equal([2, 4, 1], result.Value.Select(x => x.Version));
        _dotnetTools.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetDotnetToolByName_ReturnsTheRecordWithItsVersion()
    {
        _dotnetTools.Setup(r => r.GetByName("DOTNETEF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewDotnetTool("DotnetEf", "dotnet-ef", "9.0.8", "Entity Framework", 5));

        Result<StsDotnetToolDataModel> result = await GetByName("DOTNETEF");

        Assert.Equal("DotnetEf", result.Value.Name);
        Assert.Equal("dotnet-ef", result.Value.PackageId);
        Assert.Equal("9.0.8", result.Value.MaxVersion);
        Assert.Equal("Entity Framework", result.Value.Description);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetDotnetToolByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _dotnetTools.Setup(r => r.GetByName("ReportGenerator", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DotnetTool?)null);

        Result<StsDotnetToolDataModel> result = await GetByName("ReportGenerator");

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("DotnetTool With Name ReportGenerator Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        StsDotnetToolDataModel model = TestData.NewDotnetTool("DotnetEf", "dotnet-ef", "9.0.8", "Entity Framework", 7)
            .ToContractModel();

        Assert.Equal("DotnetEf", model.Name);
        Assert.Equal("dotnet-ef", model.PackageId);
        Assert.Equal("9.0.8", model.MaxVersion);
        Assert.Equal("Entity Framework", model.Description);
        Assert.Equal(7, model.Version);
    }
}
