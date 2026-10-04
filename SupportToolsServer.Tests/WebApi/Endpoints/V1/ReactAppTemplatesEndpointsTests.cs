using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.ReactAppTemplates.DeleteReactAppTemplate;
using SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplateByName;
using SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplates;
using SupportToolsServer.Application.ReactAppTemplates.UpdateReactAppTemplate;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class ReactAppTemplatesEndpointsTests
{
    private static Mock<ICommandHandler<UpdateReactAppTemplateCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateReactAppTemplateCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateReactAppTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return handler;
    }

    private static Mock<IQueryHandler<GetReactAppTemplateByNameQuery, StsReactAppTemplateDataModel>> GetHandler(
        Result<StsReactAppTemplateDataModel> result)
    {
        return HandlerMocks.Query<GetReactAppTemplateByNameQuery, StsReactAppTemplateDataModel>(result);
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
    public async Task UseReactAppTemplatesEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseReactAppTemplatesEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/reactapptemplates/delete/{key}", "GET api/v1/reactapptemplates",
            "GET api/v1/reactapptemplates/{key}", "POST api/v1/reactapptemplates/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseReactAppTemplatesEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseReactAppTemplatesEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseReactAppTemplatesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseReactAppTemplatesEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseReactAppTemplatesEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseReactAppTemplatesEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseReactAppTemplatesEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetReactAppTemplates_ReturnsTheListOfTheHandler()
    {
        List<StsReactAppTemplateDataModel> reactAppTemplates =
            [TestData.ReactAppTemplateModel("ReduxApp", "redux-typescript", 2)];
        var handler =
            HandlerMocks.Query<GetReactAppTemplatesQuery, List<StsReactAppTemplateDataModel>>(reactAppTemplates);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsReactAppTemplateDataModel>>, ProblemHttpResult> result =
            await ReactAppTemplatesEndpoints.GetReactAppTemplates(handler.Object, cancellation.Token);

        Assert.Same(reactAppTemplates, Assert.IsType<Ok<List<StsReactAppTemplateDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetReactAppTemplatesQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetReactAppTemplates_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetReactAppTemplatesQuery, List<StsReactAppTemplateDataModel>>(
            Result.Failure<List<StsReactAppTemplateDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsReactAppTemplateDataModel>>, ProblemHttpResult> result =
            await ReactAppTemplatesEndpoints.GetReactAppTemplates(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetReactAppTemplates_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetReactAppTemplatesQueryHandler from GetReactAppTemplates",
            () => ReactAppTemplatesEndpoints.GetReactAppTemplates(HandlerMocks
                .Query<GetReactAppTemplatesQuery, List<StsReactAppTemplateDataModel>>(
                    new List<StsReactAppTemplateDataModel>()).Object));
    }

    [Fact]
    public async Task GetReactAppTemplateByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsReactAppTemplateDataModel reactAppTemplate =
            TestData.ReactAppTemplateModel("ReduxApp", "redux-typescript", 4);
        var handler = GetHandler(reactAppTemplate);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsReactAppTemplateDataModel>, ProblemHttpResult> result =
            await ReactAppTemplatesEndpoints.GetReactAppTemplateByName("ReduxApp", handler.Object, cancellation.Token);

        Assert.Same(reactAppTemplate, Assert.IsType<Ok<StsReactAppTemplateDataModel>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(It.Is<GetReactAppTemplateByNameQuery>(q => q.Name == "ReduxApp"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetReactAppTemplateByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler =
            GetHandler(Error.NotFound("RecordWithNameNotFound", "ReactAppTemplate With Name VueApp Not Found"));

        Results<Ok<StsReactAppTemplateDataModel>, ProblemHttpResult> result =
            await ReactAppTemplatesEndpoints.GetReactAppTemplateByName("VueApp", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("ReactAppTemplate With Name VueApp Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetReactAppTemplateByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetReactAppTemplateByNameQueryHandler for ReduxApp from GetReactAppTemplateByName",
            () => ReactAppTemplatesEndpoints.GetReactAppTemplateByName("ReduxApp",
                GetHandler(TestData.ReactAppTemplateModel("ReduxApp")).Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateReactAppTemplate_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsReactAppTemplateDataModel reactAppTemplate = TestData.ReactAppTemplateModel("Other", "redux-typescript", 3);
        Mock<ICommandHandler<UpdateReactAppTemplateCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result = await ReactAppTemplatesEndpoints.UpdateReactAppTemplate(
            "ReduxApp", reactAppTemplate, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateReactAppTemplateCommand>(c =>
                    c.ReactAppTemplate == reactAppTemplate && c.ReactAppTemplate.Name == "ReduxApp" &&
                    c.ReactAppTemplate.Template == "redux-typescript" && c.ReactAppTemplate.Version == 3),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateReactAppTemplate_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateReactAppTemplateCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "ReactAppTemplate ReduxApp Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result = await ReactAppTemplatesEndpoints.UpdateReactAppTemplate("ReduxApp",
            TestData.ReactAppTemplateModel("ReduxApp", "redux-typescript", 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("ReactAppTemplate ReduxApp Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateReactAppTemplate_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateReactAppTemplateCommandHandler for ReduxApp from UpdateReactAppTemplate",
            () => ReactAppTemplatesEndpoints.UpdateReactAppTemplate("ReduxApp",
                TestData.ReactAppTemplateModel("ReduxApp"), UpdateHandler(1).Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteReactAppTemplate_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteReactAppTemplateCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result = await ReactAppTemplatesEndpoints.DeleteReactAppTemplate("ReduxApp",
            version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteReactAppTemplateCommand>(c => c.Name == "ReduxApp" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteReactAppTemplate_ReturnsRecordIsInUseAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteReactAppTemplateCommand>(Error.Conflict("RecordIsInUse",
            "ReactAppTemplate ReduxApp Is Used By: ProjectTemplate ApiWithReact"));

        Results<Ok, ProblemHttpResult> result =
            await ReactAppTemplatesEndpoints.DeleteReactAppTemplate("ReduxApp", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("RecordIsInUse", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteReactAppTemplate_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteReactAppTemplateCommandHandler for ReduxApp from DeleteReactAppTemplate",
            () => ReactAppTemplatesEndpoints.DeleteReactAppTemplate("ReduxApp", null,
                HandlerMocks.Command<DeleteReactAppTemplateCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("@scope%2Fname")]
    [InlineData("@scope%2fname")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = GetHandler(TestData.ReactAppTemplateModel("@scope/name"));
        Mock<ICommandHandler<UpdateReactAppTemplateCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteReactAppTemplateCommand>(Result.Success());
        StsReactAppTemplateDataModel body = TestData.ReactAppTemplateModel("other");

        await ReactAppTemplatesEndpoints.GetReactAppTemplateByName(key, get.Object);
        await ReactAppTemplatesEndpoints.UpdateReactAppTemplate(key, body, update.Object);
        await ReactAppTemplatesEndpoints.DeleteReactAppTemplate(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetReactAppTemplateByNameQuery>(q => q.Name == "@scope/name"),
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("@scope/name", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteReactAppTemplateCommand>(c => c.Name == "@scope/name"),
                It.IsAny<CancellationToken>()), Times.Once);
    }
}
