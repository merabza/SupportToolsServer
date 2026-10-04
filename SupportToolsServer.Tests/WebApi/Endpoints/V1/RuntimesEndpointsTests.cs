using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.Runtimes.DeleteRuntime;
using SupportToolsServer.Application.Runtimes.GetRuntimeByName;
using SupportToolsServer.Application.Runtimes.GetRuntimes;
using SupportToolsServer.Application.Runtimes.UpdateRuntime;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class RuntimesEndpointsTests
{
    private static Mock<ICommandHandler<UpdateRuntimeCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateRuntimeCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateRuntimeCommand>(), It.IsAny<CancellationToken>()))
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
    public async Task UseRuntimesEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseRuntimesEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/runtimes/delete/{key}", "GET api/v1/runtimes", "GET api/v1/runtimes/{key}",
            "POST api/v1/runtimes/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseRuntimesEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseRuntimesEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseRuntimesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseRuntimesEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseRuntimesEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseRuntimesEndpoints(null));

        List<string> protectedRoutes = await MappedRoutes.RequiringAuthorization(app => app.UseRuntimesEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetRuntimes_ReturnsTheListOfTheHandler()
    {
        List<StsRuntimeDataModel> runtimes = [TestData.RuntimeModel("win-x64", "Windows x64", 2)];
        var handler = HandlerMocks.Query<GetRuntimesQuery, List<StsRuntimeDataModel>>(runtimes);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsRuntimeDataModel>>, ProblemHttpResult> result =
            await RuntimesEndpoints.GetRuntimes(handler.Object, cancellation.Token);

        Assert.Same(runtimes, Assert.IsType<Ok<List<StsRuntimeDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetRuntimesQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetRuntimes_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetRuntimesQuery, List<StsRuntimeDataModel>>(
            Result.Failure<List<StsRuntimeDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsRuntimeDataModel>>, ProblemHttpResult> result =
            await RuntimesEndpoints.GetRuntimes(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetRuntimes_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetRuntimesQueryHandler from GetRuntimes",
            () => RuntimesEndpoints.GetRuntimes(HandlerMocks
                .Query<GetRuntimesQuery, List<StsRuntimeDataModel>>(new List<StsRuntimeDataModel>()).Object));
    }

    [Fact]
    public async Task GetRuntimeByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsRuntimeDataModel runtime = TestData.RuntimeModel("win-x64", "Windows x64", 4);
        var handler = HandlerMocks.Query<GetRuntimeByNameQuery, StsRuntimeDataModel>(runtime);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsRuntimeDataModel>, ProblemHttpResult> result =
            await RuntimesEndpoints.GetRuntimeByName("win-x64", handler.Object, cancellation.Token);

        Assert.Same(runtime, Assert.IsType<Ok<StsRuntimeDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetRuntimeByNameQuery>(q => q.Name == "win-x64"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetRuntimeByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetRuntimeByNameQuery, StsRuntimeDataModel>(
            Error.NotFound("RecordWithNameNotFound", "Runtime With Name osx-arm64 Not Found"));

        Results<Ok<StsRuntimeDataModel>, ProblemHttpResult> result =
            await RuntimesEndpoints.GetRuntimeByName("osx-arm64", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("Runtime With Name osx-arm64 Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetRuntimeByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetRuntimeByNameQueryHandler for win-x64 from GetRuntimeByName",
            () => RuntimesEndpoints.GetRuntimeByName("win-x64",
                HandlerMocks.Query<GetRuntimeByNameQuery, StsRuntimeDataModel>(TestData.RuntimeModel("win-x64"))
                    .Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateRuntime_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsRuntimeDataModel runtime = TestData.RuntimeModel("Other", "Windows x64", 3);
        Mock<ICommandHandler<UpdateRuntimeCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await RuntimesEndpoints.UpdateRuntime("win-x64", runtime, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateRuntimeCommand>(c =>
                    c.Runtime == runtime && c.Runtime.Name == "win-x64" && c.Runtime.Description == "Windows x64" &&
                    c.Runtime.Version == 3), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateRuntime_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateRuntimeCommand, int>> handler = UpdateHandler(Error.Conflict("ConcurrencyConflict",
            "Runtime win-x64 Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result =
            await RuntimesEndpoints.UpdateRuntime("win-x64", TestData.RuntimeModel("win-x64", null, 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("Runtime win-x64 Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateRuntime_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateRuntimeCommandHandler for win-x64 from UpdateRuntime",
            () => RuntimesEndpoints.UpdateRuntime("win-x64", TestData.RuntimeModel("win-x64"),
                UpdateHandler(1).Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteRuntime_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteRuntimeCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await RuntimesEndpoints.DeleteRuntime("win-x64", version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteRuntimeCommand>(c => c.Name == "win-x64" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteRuntime_ReturnsRecordIsInUseAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteRuntimeCommand>(Error.Conflict("RecordIsInUse",
            "Runtime win-x64 Is Used By: Server PAZISI"));

        Results<Ok, ProblemHttpResult> result = await RuntimesEndpoints.DeleteRuntime("win-x64", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("RecordIsInUse", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteRuntime_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteRuntimeCommandHandler for win-x64 from DeleteRuntime",
            () => RuntimesEndpoints.DeleteRuntime("win-x64", null,
                HandlerMocks.Command<DeleteRuntimeCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("@scope%2Fname")]
    [InlineData("@scope%2fname")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = HandlerMocks.Query<GetRuntimeByNameQuery, StsRuntimeDataModel>(TestData.RuntimeModel("@scope/name"));
        Mock<ICommandHandler<UpdateRuntimeCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteRuntimeCommand>(Result.Success());
        StsRuntimeDataModel body = TestData.RuntimeModel("other");

        await RuntimesEndpoints.GetRuntimeByName(key, get.Object);
        await RuntimesEndpoints.UpdateRuntime(key, body, update.Object);
        await RuntimesEndpoints.DeleteRuntime(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetRuntimeByNameQuery>(q => q.Name == "@scope/name"), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("@scope/name", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteRuntimeCommand>(c => c.Name == "@scope/name"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
