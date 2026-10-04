using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.DotnetTools.DeleteDotnetTool;
using SupportToolsServer.Application.DotnetTools.GetDotnetToolByName;
using SupportToolsServer.Application.DotnetTools.GetDotnetTools;
using SupportToolsServer.Application.DotnetTools.UpdateDotnetTool;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class DotnetToolsEndpointsTests
{
    private static Mock<ICommandHandler<UpdateDotnetToolCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateDotnetToolCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateDotnetToolCommand>(), It.IsAny<CancellationToken>()))
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
    public async Task UseDotnetToolsEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseDotnetToolsEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/dotnettools/delete/{key}", "GET api/v1/dotnettools", "GET api/v1/dotnettools/{key}",
            "POST api/v1/dotnettools/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseDotnetToolsEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseDotnetToolsEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseDotnetToolsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseDotnetToolsEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseDotnetToolsEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseDotnetToolsEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseDotnetToolsEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetDotnetTools_ReturnsTheListOfTheHandler()
    {
        List<StsDotnetToolDataModel> dotnetTools =
            [TestData.DotnetToolModel("DotnetEf", "dotnet-ef", "9.0.8", "Entity Framework", 2)];
        var handler = HandlerMocks.Query<GetDotnetToolsQuery, List<StsDotnetToolDataModel>>(dotnetTools);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsDotnetToolDataModel>>, ProblemHttpResult> result =
            await DotnetToolsEndpoints.GetDotnetTools(handler.Object, cancellation.Token);

        Assert.Same(dotnetTools, Assert.IsType<Ok<List<StsDotnetToolDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetDotnetToolsQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetDotnetTools_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetDotnetToolsQuery, List<StsDotnetToolDataModel>>(
            Result.Failure<List<StsDotnetToolDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsDotnetToolDataModel>>, ProblemHttpResult> result =
            await DotnetToolsEndpoints.GetDotnetTools(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetDotnetTools_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetDotnetToolsQueryHandler from GetDotnetTools",
            () => DotnetToolsEndpoints.GetDotnetTools(HandlerMocks
                .Query<GetDotnetToolsQuery, List<StsDotnetToolDataModel>>(new List<StsDotnetToolDataModel>()).Object));
    }

    [Fact]
    public async Task GetDotnetToolByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsDotnetToolDataModel dotnetTool = TestData.DotnetToolModel("DotnetEf", version: 4);
        var handler = HandlerMocks.Query<GetDotnetToolByNameQuery, StsDotnetToolDataModel>(dotnetTool);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsDotnetToolDataModel>, ProblemHttpResult> result =
            await DotnetToolsEndpoints.GetDotnetToolByName("DotnetEf", handler.Object, cancellation.Token);

        Assert.Same(dotnetTool, Assert.IsType<Ok<StsDotnetToolDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetDotnetToolByNameQuery>(q => q.Name == "DotnetEf"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetDotnetToolByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetDotnetToolByNameQuery, StsDotnetToolDataModel>(
            Error.NotFound("RecordWithNameNotFound", "DotnetTool With Name ReportGenerator Not Found"));

        Results<Ok<StsDotnetToolDataModel>, ProblemHttpResult> result =
            await DotnetToolsEndpoints.GetDotnetToolByName("ReportGenerator", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("DotnetTool With Name ReportGenerator Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetDotnetToolByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetDotnetToolByNameQueryHandler for DotnetEf from GetDotnetToolByName",
            () => DotnetToolsEndpoints.GetDotnetToolByName("DotnetEf",
                HandlerMocks
                    .Query<GetDotnetToolByNameQuery, StsDotnetToolDataModel>(TestData.DotnetToolModel("DotnetEf"))
                    .Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateDotnetTool_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsDotnetToolDataModel dotnetTool =
            TestData.DotnetToolModel("Other", "dotnet-ef", "9.0.8", "Entity Framework", 3);
        Mock<ICommandHandler<UpdateDotnetToolCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await DotnetToolsEndpoints.UpdateDotnetTool("DotnetEf", dotnetTool, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateDotnetToolCommand>(c =>
                    c.DotnetTool == dotnetTool && c.DotnetTool.Name == "DotnetEf" &&
                    c.DotnetTool.PackageId == "dotnet-ef" && c.DotnetTool.MaxVersion == "9.0.8" &&
                    c.DotnetTool.Description == "Entity Framework" && c.DotnetTool.Version == 3), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDotnetTool_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateDotnetToolCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "DotnetTool DotnetEf Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result = await DotnetToolsEndpoints.UpdateDotnetTool("DotnetEf",
            TestData.DotnetToolModel("DotnetEf", version: 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("DotnetTool DotnetEf Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateDotnetTool_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateDotnetToolCommandHandler for DotnetEf from UpdateDotnetTool",
            () => DotnetToolsEndpoints.UpdateDotnetTool("DotnetEf", TestData.DotnetToolModel("DotnetEf"),
                UpdateHandler(1).Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteDotnetTool_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteDotnetToolCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await DotnetToolsEndpoints.DeleteDotnetTool("DotnetEf", version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteDotnetToolCommand>(c => c.Name == "DotnetEf" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteDotnetTool_ReturnsConcurrencyConflictAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteDotnetToolCommand>(Error.Conflict("ConcurrencyConflict",
            "DotnetTool DotnetEf Version Conflict: Expected 1, Actual 2"));

        Results<Ok, ProblemHttpResult> result =
            await DotnetToolsEndpoints.DeleteDotnetTool("DotnetEf", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteDotnetTool_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteDotnetToolCommandHandler for DotnetEf from DeleteDotnetTool",
            () => DotnetToolsEndpoints.DeleteDotnetTool("DotnetEf", null,
                HandlerMocks.Command<DeleteDotnetToolCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("@scope%2Fname")]
    [InlineData("@scope%2fname")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = HandlerMocks.Query<GetDotnetToolByNameQuery, StsDotnetToolDataModel>(
            TestData.DotnetToolModel("@scope/name"));
        Mock<ICommandHandler<UpdateDotnetToolCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteDotnetToolCommand>(Result.Success());
        StsDotnetToolDataModel body = TestData.DotnetToolModel("other");

        await DotnetToolsEndpoints.GetDotnetToolByName(key, get.Object);
        await DotnetToolsEndpoints.UpdateDotnetTool(key, body, update.Object);
        await DotnetToolsEndpoints.DeleteDotnetTool(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetDotnetToolByNameQuery>(q => q.Name == "@scope/name"), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("@scope/name", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteDotnetToolCommand>(c => c.Name == "@scope/name"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
