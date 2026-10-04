using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.SmartSchemas;
using SupportToolsServer.Application.SmartSchemas.GetSmartSchemaByName;
using SupportToolsServer.Application.SmartSchemas.GetSmartSchemas;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.SmartSchemas;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.SmartSchemas;

public sealed class SmartSchemaQueryHandlersTests
{
    private readonly Mock<ISmartSchemaRepository> _smartSchemas = new();

    private Task<Result<StsSmartSchemaDataModel>> GetByName(string name)
    {
        return new GetSmartSchemaByNameQueryHandler(_smartSchemas.Object).Handle(new GetSmartSchemaByNameQuery(name),
            CancellationToken.None);
    }

    //The order ignores case, so it differs from the ordinal one
    [Fact]
    public async Task GetSmartSchemas_ReturnsEveryFieldTheDetailsAndTheVersionInNameOrder()
    {
        _smartSchemas.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewSmartSchema("Reduce", 2, [("Day", 2)], 3),
            TestData.NewSmartSchema("hourly", 1, [("Hour", 48)], 4),
            TestData.NewSmartSchema("DailyStandard", version: 2)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsSmartSchemaDataModel>> result =
            await new GetSmartSchemasQueryHandler(_smartSchemas.Object).Handle(new GetSmartSchemasQuery(),
                cancellation.Token);

        Assert.Equal(["DailyStandard", "hourly", "Reduce"], result.Value.Select(x => x.Name));
        Assert.Equal([1, 1, 2], result.Value.Select(x => x.LastPreserveCount));
        Assert.Equal(["", "Hour", "Day"],
            result.Value.Select(x => string.Join(",", x.Details.Select(y => y.PeriodType))));
        Assert.Equal([2, 4, 3], result.Value.Select(x => x.Version));
        _smartSchemas.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetSmartSchemaByName_ReturnsTheRecordWithItsDetailsAndVersion()
    {
        _smartSchemas.Setup(r => r.GetByName("REDUCE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewSmartSchema("Reduce", 2, [("Day", 3)], 5));

        Result<StsSmartSchemaDataModel> result = await GetByName("REDUCE");

        Assert.Equal("Reduce", result.Value.Name);
        Assert.Equal(2, result.Value.LastPreserveCount);
        StsSmartSchemaDetailDataModel detail = Assert.Single(result.Value.Details);
        Assert.Equal("Day", detail.PeriodType);
        Assert.Equal(3, detail.PreserveCount);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetSmartSchemaByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _smartSchemas.Setup(r => r.GetByName("Hourly", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartSchema?)null);

        Result<StsSmartSchemaDataModel> result = await GetByName("Hourly");

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("SmartSchema With Name Hourly Not Found", result.Error.Description);
    }

    //The order of the details means nothing, so the contract sorts them by the period type, ignoring case, for a
    //stable hash on the client
    [Fact]
    public void ToContractModel_CopiesEveryFieldWithTheDetailsInPeriodTypeOrder()
    {
        StsSmartSchemaDataModel model = TestData
            .NewSmartSchema("Reduce", 2, [("Year", 1), ("day", 7), ("Month", 3), ("Hour", 48)], 7).ToContractModel();

        Assert.Equal("Reduce", model.Name);
        Assert.Equal(2, model.LastPreserveCount);
        Assert.Equal(["day", "Hour", "Month", "Year"], model.Details.Select(x => x.PeriodType));
        Assert.Equal([7, 48, 3, 1], model.Details.Select(x => x.PreserveCount));
        Assert.Equal(7, model.Version);
    }
}
