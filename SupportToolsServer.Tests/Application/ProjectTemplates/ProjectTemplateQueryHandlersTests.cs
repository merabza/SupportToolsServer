using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SupportToolsServer.Application.ProjectTemplates;
using SupportToolsServer.Application.ProjectTemplates.GetProjectTemplateByName;
using SupportToolsServer.Application.ProjectTemplates.GetProjectTemplates;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Application.ProjectTemplates;

public sealed class ProjectTemplateQueryHandlersTests
{
    private readonly Mock<IProjectTemplateRepository> _projectTemplates = new();
    private readonly ReactAppTemplate _reactTemplate = TestData.NewReactAppTemplate("redux-typescript");
    private readonly Mock<IReactAppTemplateRepository> _reactAppTemplates = new();

    public ProjectTemplateQueryHandlersTests()
    {
        _reactAppTemplates.Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.NewReactAppTemplate("typescript"), _reactTemplate]);
    }

    private Task<Result<StsProjectTemplateDataModel>> GetByName(string name)
    {
        return new GetProjectTemplateByNameQueryHandler(_projectTemplates.Object, _reactAppTemplates.Object).Handle(
            new GetProjectTemplateByNameQuery(name), CancellationToken.None);
    }

    //The order ignores case, so it differs from the ordinal one; the React template is named, not given by its id
    [Fact]
    public async Task GetProjectTemplates_ReturnsEveryRecordWithTheNameOfItsReactTemplateInNameOrder()
    {
        _projectTemplates.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync([
            TestData.NewProjectTemplate("Service", version: 2),
            TestData.NewProjectTemplate("Reactredux", _reactTemplate, 4),
            TestData.NewProjectTemplate("console")
        ]);
        using var cancellation = new CancellationTokenSource();

        Result<List<StsProjectTemplateDataModel>> result =
            await new GetProjectTemplatesQueryHandler(_projectTemplates.Object, _reactAppTemplates.Object).Handle(
                new GetProjectTemplatesQuery(), cancellation.Token);

        Assert.Equal(["console", "Reactredux", "Service"], result.Value.Select(x => x.Name));
        Assert.Equal([null, "redux-typescript", null], result.Value.Select(x => x.ReactTemplateName));
        Assert.Equal([1, 4, 2], result.Value.Select(x => x.Version));
        _projectTemplates.Verify(r => r.GetAll(cancellation.Token), Times.Once);
        _reactAppTemplates.Verify(r => r.GetAll(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetProjectTemplateByName_ReturnsTheRecordWithTheNameOfItsReactTemplateAndItsVersion()
    {
        _projectTemplates.Setup(r => r.GetByName("REACTREDUX", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.NewProjectTemplate("Reactredux", _reactTemplate, 5));

        Result<StsProjectTemplateDataModel> result = await GetByName("REACTREDUX");

        Assert.Equal("Reactredux", result.Value.Name);
        Assert.Equal("redux-typescript", result.Value.ReactTemplateName);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetProjectTemplateByName_ReturnsRecordWithNameNotFound_WhenThereIsNoSuchName()
    {
        _projectTemplates.Setup(r => r.GetByName("Vue", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTemplate?)null);

        Result<StsProjectTemplateDataModel> result = await GetByName("Vue");

        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("ProjectTemplate With Name Vue Not Found", result.Error.Description);
    }

    [Fact]
    public void ToContractModel_CopiesEveryField()
    {
        var projectTemplate = new ProjectTemplate(ProjectTemplateId.CreateUnique(), "Reactredux", "Api", "ReactTest",
            "Rt", true, false, true, false, true, false, true, false, true, false, _reactTemplate.Id, 7);

        StsProjectTemplateDataModel model = projectTemplate.ToContractModel(
            new Dictionary<ReactAppTemplateId, string> { [_reactTemplate.Id] = "redux-typescript" });

        Assert.Equal("Reactredux", model.Name);
        Assert.Equal("Api", model.SupportProjectType);
        Assert.Equal("ReactTest", model.TestProjectName);
        Assert.Equal("Rt", model.TestProjectShortName);
        Assert.Equal([true, false, true, false, true, false, true, false, true, false],
        [
            model.UseDatabase, model.UseDbPartFolderForDatabaseProjects, model.UseMenu, model.UseHttps,
            model.UseReact, model.UseCarcass, model.UseIdentity, model.UseReCounter, model.UseSignalR,
            model.UseFluentValidation
        ]);
        Assert.Equal("redux-typescript", model.ReactTemplateName);
        Assert.Equal(7, model.Version);
    }

    //The flags are copied one by one: each one alone is true
    [Fact]
    public void ToContractModel_CopiesEveryFlagByItself()
    {
        var names = new Dictionary<ReactAppTemplateId, string>();
        for (int flag = 0; flag < 10; flag++)
        {
            bool[] flags = [.. Enumerable.Range(0, 10).Select(x => x == flag)];
            var projectTemplate = new ProjectTemplate(ProjectTemplateId.CreateUnique(), "Console", "Console", null,
                null, flags[0], flags[1], flags[2], flags[3], flags[4], flags[5], flags[6], flags[7], flags[8],
                flags[9], null, 1);

            StsProjectTemplateDataModel model = projectTemplate.ToContractModel(names);

            bool[] copied =
            [
                model.UseDatabase, model.UseDbPartFolderForDatabaseProjects, model.UseMenu, model.UseHttps,
                model.UseReact, model.UseCarcass, model.UseIdentity, model.UseReCounter, model.UseSignalR,
                model.UseFluentValidation
            ];
            Assert.Equal(flags, copied);
            Assert.Null(model.ReactTemplateName);
        }
    }
}
