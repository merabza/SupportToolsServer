using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.Environments.DeleteEnvironment;
using SupportToolsServer.Application.Environments.GetEnvironmentByName;
using SupportToolsServer.Application.Environments.GetEnvironments;
using SupportToolsServer.Application.Environments.UpdateEnvironment;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class EnvironmentsEndpointsTests
{
    private static Mock<ICommandHandler<UpdateEnvironmentCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateEnvironmentCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateEnvironmentCommand>(), It.IsAny<CancellationToken>()))
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
    public async Task UseEnvironmentsEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseEnvironmentsEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/environments/delete/{key}", "GET api/v1/environments", "GET api/v1/environments/{key}",
            "POST api/v1/environments/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseEnvironmentsEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseEnvironmentsEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseEnvironmentsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseEnvironmentsEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseEnvironmentsEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseEnvironmentsEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseEnvironmentsEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetEnvironments_ReturnsTheListOfTheHandler()
    {
        List<StsEnvironmentDataModel> environments = [TestData.EnvironmentModel("Prod", "Production", 2)];
        var handler = HandlerMocks.Query<GetEnvironmentsQuery, List<StsEnvironmentDataModel>>(environments);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsEnvironmentDataModel>>, ProblemHttpResult> result =
            await EnvironmentsEndpoints.GetEnvironments(handler.Object, cancellation.Token);

        Assert.Same(environments, Assert.IsType<Ok<List<StsEnvironmentDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetEnvironmentsQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetEnvironments_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetEnvironmentsQuery, List<StsEnvironmentDataModel>>(
            Result.Failure<List<StsEnvironmentDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsEnvironmentDataModel>>, ProblemHttpResult> result =
            await EnvironmentsEndpoints.GetEnvironments(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetEnvironments_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetEnvironmentsQueryHandler from GetEnvironments",
            () => EnvironmentsEndpoints.GetEnvironments(HandlerMocks
                .Query<GetEnvironmentsQuery, List<StsEnvironmentDataModel>>(new List<StsEnvironmentDataModel>())
                .Object));
    }

    [Fact]
    public async Task GetEnvironmentByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsEnvironmentDataModel environment = TestData.EnvironmentModel("Prod", "Production", 4);
        var handler = HandlerMocks.Query<GetEnvironmentByNameQuery, StsEnvironmentDataModel>(environment);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsEnvironmentDataModel>, ProblemHttpResult> result =
            await EnvironmentsEndpoints.GetEnvironmentByName("Prod", handler.Object, cancellation.Token);

        Assert.Same(environment, Assert.IsType<Ok<StsEnvironmentDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetEnvironmentByNameQuery>(q => q.Name == "Prod"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetEnvironmentByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetEnvironmentByNameQuery, StsEnvironmentDataModel>(
            Error.NotFound("RecordWithNameNotFound", "Environment With Name Stage Not Found"));

        Results<Ok<StsEnvironmentDataModel>, ProblemHttpResult> result =
            await EnvironmentsEndpoints.GetEnvironmentByName("Stage", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("Environment With Name Stage Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetEnvironmentByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetEnvironmentByNameQueryHandler for Prod from GetEnvironmentByName",
            () => EnvironmentsEndpoints.GetEnvironmentByName("Prod",
                HandlerMocks
                    .Query<GetEnvironmentByNameQuery, StsEnvironmentDataModel>(TestData.EnvironmentModel("Prod"))
                    .Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateEnvironment_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsEnvironmentDataModel environment = TestData.EnvironmentModel("Other", "Production", 3);
        Mock<ICommandHandler<UpdateEnvironmentCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await EnvironmentsEndpoints.UpdateEnvironment("Prod", environment, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateEnvironmentCommand>(c =>
                    c.Environment == environment && c.Environment.Name == "Prod" &&
                    c.Environment.Description == "Production" && c.Environment.Version == 3), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task UpdateEnvironment_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateEnvironmentCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "Environment Prod Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result =
            await EnvironmentsEndpoints.UpdateEnvironment("Prod", TestData.EnvironmentModel("Prod", null, 2),
                handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("Environment Prod Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateEnvironment_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateEnvironmentCommandHandler for Prod from UpdateEnvironment",
            () => EnvironmentsEndpoints.UpdateEnvironment("Prod", TestData.EnvironmentModel("Prod"),
                UpdateHandler(1).Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteEnvironment_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteEnvironmentCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await EnvironmentsEndpoints.DeleteEnvironment("Prod", version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteEnvironmentCommand>(c => c.Name == "Prod" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteEnvironment_ReturnsRecordIsInUseAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteEnvironmentCommand>(Error.Conflict("RecordIsInUse",
            "Environment Prod Is Used By: ServerInfo AppA"));

        Results<Ok, ProblemHttpResult>
            result = await EnvironmentsEndpoints.DeleteEnvironment("Prod", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("RecordIsInUse", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteEnvironment_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteEnvironmentCommandHandler for Prod from DeleteEnvironment",
            () => EnvironmentsEndpoints.DeleteEnvironment("Prod", null,
                HandlerMocks.Command<DeleteEnvironmentCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("@scope%2Fname")]
    [InlineData("@scope%2fname")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = HandlerMocks.Query<GetEnvironmentByNameQuery, StsEnvironmentDataModel>(
            TestData.EnvironmentModel("@scope/name"));
        Mock<ICommandHandler<UpdateEnvironmentCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteEnvironmentCommand>(Result.Success());
        StsEnvironmentDataModel body = TestData.EnvironmentModel("other");

        await EnvironmentsEndpoints.GetEnvironmentByName(key, get.Object);
        await EnvironmentsEndpoints.UpdateEnvironment(key, body, update.Object);
        await EnvironmentsEndpoints.DeleteEnvironment(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetEnvironmentByNameQuery>(q => q.Name == "@scope/name"),
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("@scope/name", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteEnvironmentCommand>(c => c.Name == "@scope/name"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
