using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;
using SupportToolsServer.Application.SmartSchemas.GetSmartSchemaByName;
using SupportToolsServer.Application.SmartSchemas.GetSmartSchemas;
using SupportToolsServer.Application.SmartSchemas.UpdateSmartSchema;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class SmartSchemasEndpointsTests
{
    private static Mock<ICommandHandler<UpdateSmartSchemaCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateSmartSchemaCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateSmartSchemaCommand>(), It.IsAny<CancellationToken>()))
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
    public async Task UseSmartSchemasEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseSmartSchemasEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/smartschemas/delete/{key}", "GET api/v1/smartschemas", "GET api/v1/smartschemas/{key}",
            "POST api/v1/smartschemas/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseSmartSchemasEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseSmartSchemasEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseSmartSchemasEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseSmartSchemasEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseSmartSchemasEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseSmartSchemasEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseSmartSchemasEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetSmartSchemas_ReturnsTheListOfTheHandler()
    {
        List<StsSmartSchemaDataModel> smartSchemas = [TestData.SmartSchemaModel("Reduce", 1, [("Day", 2)], 2)];
        var handler = HandlerMocks.Query<GetSmartSchemasQuery, List<StsSmartSchemaDataModel>>(smartSchemas);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsSmartSchemaDataModel>>, ProblemHttpResult> result =
            await SmartSchemasEndpoints.GetSmartSchemas(handler.Object, cancellation.Token);

        Assert.Same(smartSchemas, Assert.IsType<Ok<List<StsSmartSchemaDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetSmartSchemasQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetSmartSchemas_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetSmartSchemasQuery, List<StsSmartSchemaDataModel>>(
            Result.Failure<List<StsSmartSchemaDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsSmartSchemaDataModel>>, ProblemHttpResult> result =
            await SmartSchemasEndpoints.GetSmartSchemas(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetSmartSchemas_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetSmartSchemasQueryHandler from GetSmartSchemas",
            () => SmartSchemasEndpoints.GetSmartSchemas(HandlerMocks
                .Query<GetSmartSchemasQuery, List<StsSmartSchemaDataModel>>(new List<StsSmartSchemaDataModel>())
                .Object));
    }

    [Fact]
    public async Task GetSmartSchemaByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsSmartSchemaDataModel smartSchema = TestData.SmartSchemaModel("Reduce", version: 4);
        var handler = HandlerMocks.Query<GetSmartSchemaByNameQuery, StsSmartSchemaDataModel>(smartSchema);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsSmartSchemaDataModel>, ProblemHttpResult> result =
            await SmartSchemasEndpoints.GetSmartSchemaByName("Reduce", handler.Object, cancellation.Token);

        Assert.Same(smartSchema, Assert.IsType<Ok<StsSmartSchemaDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetSmartSchemaByNameQuery>(q => q.Name == "Reduce"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetSmartSchemaByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetSmartSchemaByNameQuery, StsSmartSchemaDataModel>(
            Error.NotFound("RecordWithNameNotFound", "SmartSchema With Name Hourly Not Found"));

        Results<Ok<StsSmartSchemaDataModel>, ProblemHttpResult> result =
            await SmartSchemasEndpoints.GetSmartSchemaByName("Hourly", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("SmartSchema With Name Hourly Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetSmartSchemaByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetSmartSchemaByNameQueryHandler for Reduce from GetSmartSchemaByName",
            () => SmartSchemasEndpoints.GetSmartSchemaByName("Reduce",
                HandlerMocks
                    .Query<GetSmartSchemaByNameQuery, StsSmartSchemaDataModel>(TestData.SmartSchemaModel("Reduce"))
                    .Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateSmartSchema_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsSmartSchemaDataModel smartSchema = TestData.SmartSchemaModel("Other", 2, [("Day", 3)], 3);
        Mock<ICommandHandler<UpdateSmartSchemaCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await SmartSchemasEndpoints.UpdateSmartSchema("Reduce", smartSchema, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateSmartSchemaCommand>(c =>
                    c.SmartSchema == smartSchema && c.SmartSchema.Name == "Reduce" &&
                    c.SmartSchema.LastPreserveCount == 2 && c.SmartSchema.Details.Count == 1 &&
                    c.SmartSchema.Version == 3), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateSmartSchema_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateSmartSchemaCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "SmartSchema Reduce Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result = await SmartSchemasEndpoints.UpdateSmartSchema("Reduce",
            TestData.SmartSchemaModel("Reduce", version: 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("SmartSchema Reduce Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateSmartSchema_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateSmartSchemaCommandHandler for Reduce from UpdateSmartSchema",
            () => SmartSchemasEndpoints.UpdateSmartSchema("Reduce", TestData.SmartSchemaModel("Reduce"),
                UpdateHandler(1).Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteSmartSchema_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteSmartSchemaCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await SmartSchemasEndpoints.DeleteSmartSchema("Reduce", version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteSmartSchemaCommand>(c => c.Name == "Reduce" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteSmartSchema_ReturnsConcurrencyConflictAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteSmartSchemaCommand>(Error.Conflict("ConcurrencyConflict",
            "SmartSchema Reduce Version Conflict: Expected 1, Actual 2"));

        Results<Ok, ProblemHttpResult> result =
            await SmartSchemasEndpoints.DeleteSmartSchema("Reduce", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteSmartSchema_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteSmartSchemaCommandHandler for Reduce from DeleteSmartSchema",
            () => SmartSchemasEndpoints.DeleteSmartSchema("Reduce", null,
                HandlerMocks.Command<DeleteSmartSchemaCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("Daily%2FStandard")]
    [InlineData("Daily%2fStandard")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = HandlerMocks.Query<GetSmartSchemaByNameQuery, StsSmartSchemaDataModel>(
            TestData.SmartSchemaModel("Daily/Standard"));
        Mock<ICommandHandler<UpdateSmartSchemaCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteSmartSchemaCommand>(Result.Success());
        StsSmartSchemaDataModel body = TestData.SmartSchemaModel("other");

        await SmartSchemasEndpoints.GetSmartSchemaByName(key, get.Object);
        await SmartSchemasEndpoints.UpdateSmartSchema(key, body, update.Object);
        await SmartSchemasEndpoints.DeleteSmartSchema(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetSmartSchemaByNameQuery>(q => q.Name == "Daily/Standard"),
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Daily/Standard", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteSmartSchemaCommand>(c => c.Name == "Daily/Standard"),
                It.IsAny<CancellationToken>()), Times.Once);
    }
}
