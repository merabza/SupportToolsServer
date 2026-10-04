using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.ReactAppTemplates;
using SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplateByName;
using SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplates;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ReactAppTemplates;

public sealed class ReactAppTemplateQueryHandlersTests
{
    private readonly Mock<IReactAppTemplateRepository> _reactAppTemplates = new();

    private Task<Result<StsReactAppTemplateDataModel>> GetByName(string name)
    {
        return new GetReactAppTemplateByNameQueryHandler(_reactAppTemplates.Object).Handle(
            new GetReactAppTemplateByNameQuery(name), CancellationToken.None);
    }

    //The order ignores case, so it differs from the ordinal one
    [Fact]
    public async Task GetReactAppTemplates_ReturnsEveryFieldAndTheVersionInNameOrder()
    {
        _reactAppTemplates.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewReactAppTemplate("web", "cra-template-web"),
            TestData.NewReactAppTemplate("app", "cra-template-app", 4),
            TestData.NewReactAppTemplate("Redux", "redux", 2)
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsReactAppTemplateDataModel>> result =
            await new GetReactAppTemplatesQueryHandler(_reactAppTemplates.Object).Handle(
                new GetReactAppTemplatesQuery(), cancellation.Token);

        Assert.Equal(["app", "Redux", "web"], result.Value.Select(x => x.Name));
        Assert.Equal(["cra-template-app", "redux", "cra-template-web"], result.Value.Select(x => x.Template));
        Assert.Equal([4, 2, 1], result.Value.Select(x => x.Version));
        _reactAppTemplates.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetReactAppTemplateByName_ReturnsTheRecordWithItsVersion()
    {
        _reactAppTemplates.Setup(r => r.GetByName("REDUXAPP", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewReactAppTemplate("ReduxApp", "redux-typescript", 5));

        Result<StsReactAppTemplateDataModel> result = await GetByName("REDUXAPP");

        Assert.Equal("ReduxApp", result.Value.Name);
        Assert.Equal("redux-typescript", result.Value.Template);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetReactAppTemplateByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _reactAppTemplates.Setup(r => r.GetByName("VueApp", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReactAppTemplate?)null);

        Result<StsReactAppTemplateDataModel> result = await GetByName("VueApp");

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ReactAppTemplate With Name VueApp Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        StsReactAppTemplateDataModel model = TestData.NewReactAppTemplate("ReduxApp", "redux-typescript", 7)
            .ToContractModel();

        Assert.Equal("ReduxApp", model.Name);
        Assert.Equal("redux-typescript", model.Template);
        Assert.Equal(7, model.Version);
    }
}
