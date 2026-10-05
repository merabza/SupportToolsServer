using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.ProjectTemplates.DeleteProjectTemplate;
using SupportToolsServer.Application.ProjectTemplates.GetProjectTemplateByName;
using SupportToolsServer.Application.ProjectTemplates.GetProjectTemplates;
using SupportToolsServer.Application.ProjectTemplates.UpdateProjectTemplate;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class ProjectTemplatesEndpointsTests
{
    private static Mock<ICommandHandler<UpdateProjectTemplateCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateProjectTemplateCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateProjectTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return handler;
    }

    //Debug.WriteLine goes to the Trace listeners and Release builds leave it out, so there the line must be missing.
    //Other tests trace in parallel, so only this line is looked for
    private static async Task AssertDebugTrace(string expectedLine, Func<Task> call)
    {
        using var trace = new CollectingTraceListener();
        Trace.Listeners.Add(trace);
        try
        {
            await call();
        }
        finally
        {
            Trace.Listeners.Remove(trace);
        }

#if DEBUG
        Assert.Contains(expectedLine, trace.Lines);
#else
        Assert.DoesNotContain(expectedLine, trace.Lines);
#endif
    }

    [Fact]
    public async Task UseProjectTemplatesEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseProjectTemplatesEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/projecttemplates/delete/{key}", "GET api/v1/projecttemplates",
            "GET api/v1/projecttemplates/{key}", "POST api/v1/projecttemplates/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseProjectTemplatesEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseProjectTemplatesEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseProjectTemplatesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseProjectTemplatesEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseProjectTemplatesEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseProjectTemplatesEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseProjectTemplatesEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetProjectTemplates_ReturnsTheListOfTheHandler()
    {
        List<StsProjectTemplateDataModel> projectTemplates = [TestData.ProjectTemplateModel("Console", version: 2)];
        var handler = HandlerMocks.Query<GetProjectTemplatesQuery, List<StsProjectTemplateDataModel>>(
            projectTemplates);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsProjectTemplateDataModel>>, ProblemHttpResult> result =
            await ProjectTemplatesEndpoints.GetProjectTemplates(handler.Object, cancellation.Token);

        Assert.Same(projectTemplates, Assert.IsType<Ok<List<StsProjectTemplateDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetProjectTemplatesQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetProjectTemplates_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetProjectTemplatesQuery, List<StsProjectTemplateDataModel>>(
            Result.Failure<List<StsProjectTemplateDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsProjectTemplateDataModel>>, ProblemHttpResult> result =
            await ProjectTemplatesEndpoints.GetProjectTemplates(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetProjectTemplates_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetProjectTemplatesQueryHandler from GetProjectTemplates",
            () => ProjectTemplatesEndpoints.GetProjectTemplates(HandlerMocks
                .Query<GetProjectTemplatesQuery, List<StsProjectTemplateDataModel>>(
                    new List<StsProjectTemplateDataModel>()).Object));
    }

    [Fact]
    public async Task GetProjectTemplateByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsProjectTemplateDataModel projectTemplate =
            TestData.ProjectTemplateModel("Console With Database", version: 4);
        var handler = HandlerMocks.Query<GetProjectTemplateByNameQuery, StsProjectTemplateDataModel>(projectTemplate);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsProjectTemplateDataModel>, ProblemHttpResult> result =
            await ProjectTemplatesEndpoints.GetProjectTemplateByName("Console With Database", handler.Object,
                cancellation.Token);

        Assert.Same(projectTemplate, Assert.IsType<Ok<StsProjectTemplateDataModel>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(It.Is<GetProjectTemplateByNameQuery>(q => q.Name == "Console With Database"),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetProjectTemplateByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetProjectTemplateByNameQuery, StsProjectTemplateDataModel>(
            Error.NotFound("RecordWithNameNotFound", "ProjectTemplate With Name Vue Not Found"));

        Results<Ok<StsProjectTemplateDataModel>, ProblemHttpResult> result =
            await ProjectTemplatesEndpoints.GetProjectTemplateByName("Vue", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("ProjectTemplate With Name Vue Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetProjectTemplateByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetProjectTemplateByNameQueryHandler for Console from GetProjectTemplateByName",
            () => ProjectTemplatesEndpoints.GetProjectTemplateByName("Console",
                HandlerMocks.Query<GetProjectTemplateByNameQuery, StsProjectTemplateDataModel>(
                    TestData.ProjectTemplateModel("Console")).Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateProjectTemplate_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsProjectTemplateDataModel projectTemplate =
            TestData.ProjectTemplateModel("Other", "redux-typescript", 3);
        Mock<ICommandHandler<UpdateProjectTemplateCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await ProjectTemplatesEndpoints.UpdateProjectTemplate("Reactredux", projectTemplate, handler.Object,
                cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateProjectTemplateCommand>(c =>
                    c.ProjectTemplate == projectTemplate && c.ProjectTemplate.Name == "Reactredux" &&
                    c.ProjectTemplate.ReactTemplateName == "redux-typescript" && c.ProjectTemplate.Version == 3),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateProjectTemplate_ReturnsReferencedRecordsNotFoundAsAProblem()
    {
        Mock<ICommandHandler<UpdateProjectTemplateCommand, int>> handler = UpdateHandler(Error.NotFound(
            "ReferencedRecordsNotFound", "Referenced ReactAppTemplate Records Not Found: vue"));

        Results<Ok<int>, ProblemHttpResult> result = await ProjectTemplatesEndpoints.UpdateProjectTemplate("Vue",
            TestData.ProjectTemplateModel("Vue", "vue"), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("ReferencedRecordsNotFound", problem.ProblemDetails.Title);
        Assert.Equal("Referenced ReactAppTemplate Records Not Found: vue", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateProjectTemplate_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateProjectTemplateCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "ProjectTemplate Console Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result = await ProjectTemplatesEndpoints.UpdateProjectTemplate("Console",
            TestData.ProjectTemplateModel("Console", version: 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task UpdateProjectTemplate_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateProjectTemplateCommandHandler for Console from UpdateProjectTemplate",
            () => ProjectTemplatesEndpoints.UpdateProjectTemplate("Console", TestData.ProjectTemplateModel("Console"),
                UpdateHandler(1).Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteProjectTemplate_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteProjectTemplateCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await ProjectTemplatesEndpoints.DeleteProjectTemplate("Console", version, handler.Object,
                cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteProjectTemplateCommand>(c => c.Name == "Console" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteProjectTemplate_ReturnsConcurrencyConflictAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteProjectTemplateCommand>(Error.Conflict("ConcurrencyConflict",
            "ProjectTemplate Console Version Conflict: Expected 1, Actual 2"));

        Results<Ok, ProblemHttpResult> result =
            await ProjectTemplatesEndpoints.DeleteProjectTemplate("Console", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteProjectTemplate_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteProjectTemplateCommandHandler for Console from DeleteProjectTemplate",
            () => ProjectTemplatesEndpoints.DeleteProjectTemplate("Console", null,
                HandlerMocks.Command<DeleteProjectTemplateCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("Console%2FOld")]
    [InlineData("Console%2fOld")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = HandlerMocks.Query<GetProjectTemplateByNameQuery, StsProjectTemplateDataModel>(
            TestData.ProjectTemplateModel("Console/Old"));
        Mock<ICommandHandler<UpdateProjectTemplateCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteProjectTemplateCommand>(Result.Success());
        StsProjectTemplateDataModel body = TestData.ProjectTemplateModel("other");

        await ProjectTemplatesEndpoints.GetProjectTemplateByName(key, get.Object);
        await ProjectTemplatesEndpoints.UpdateProjectTemplate(key, body, update.Object);
        await ProjectTemplatesEndpoints.DeleteProjectTemplate(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetProjectTemplateByNameQuery>(q => q.Name == "Console/Old"),
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Console/Old", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteProjectTemplateCommand>(c => c.Name == "Console/Old"),
                It.IsAny<CancellationToken>()), Times.Once);
    }
}
