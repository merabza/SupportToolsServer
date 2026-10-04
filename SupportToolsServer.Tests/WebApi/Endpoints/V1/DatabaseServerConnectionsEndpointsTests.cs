using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;
using SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnectionByName;
using SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnections;
using SupportToolsServer.Application.DatabaseServerConnections.UpdateDatabaseServerConnection;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class DatabaseServerConnectionsEndpointsTests
{
    private static Mock<ICommandHandler<UpdateDatabaseServerConnectionCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateDatabaseServerConnectionCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateDatabaseServerConnectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return handler;
    }

    private static Mock<IQueryHandler<GetDatabaseServerConnectionByNameQuery, StsDatabaseServerConnectionDataModel>>
        GetHandler(Result<StsDatabaseServerConnectionDataModel> result)
    {
        return HandlerMocks.Query<GetDatabaseServerConnectionByNameQuery, StsDatabaseServerConnectionDataModel>(
            result);
    }

    private static async Task<IReadOnlyCollection<string>> TraceOf(Func<Task> call)
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

        return [.. trace.Lines];
    }

    //Debug.WriteLine goes to the Trace listeners and Release builds leave it out, so there the line must be missing.
    //Other tests trace in parallel, so only this line is looked for
    private static async Task AssertDebugTrace(string expectedLine, Func<Task> call)
    {
        IReadOnlyCollection<string> lines = await TraceOf(call);

#if DEBUG
        Assert.Contains(expectedLine, lines);
#else
        Assert.DoesNotContain(expectedLine, lines);
#endif
    }

    [Fact]
    public async Task UseDatabaseServerConnectionsEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) =
            await MappedRoutes.Of(app => app.UseDatabaseServerConnectionsEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/databaseserverconnections/delete/{key}", "GET api/v1/databaseserverconnections",
            "GET api/v1/databaseserverconnections/{key}", "POST api/v1/databaseserverconnections/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseDatabaseServerConnectionsEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseDatabaseServerConnectionsEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseDatabaseServerConnectionsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseDatabaseServerConnectionsEndpoints"),
            Times.Once);
    }

    [Fact]
    public async Task UseDatabaseServerConnectionsEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseDatabaseServerConnectionsEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseDatabaseServerConnectionsEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetDatabaseServerConnections_ReturnsTheListOfTheHandler()
    {
        List<StsDatabaseServerConnectionDataModel> connections =
            [TestData.DatabaseServerConnectionModel("Pc1.Sql", version: 2)];
        var handler =
            HandlerMocks.Query<GetDatabaseServerConnectionsQuery, List<StsDatabaseServerConnectionDataModel>>(
                connections);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsDatabaseServerConnectionDataModel>>, ProblemHttpResult> result =
            await DatabaseServerConnectionsEndpoints.GetDatabaseServerConnections(handler.Object, cancellation.Token);

        Assert.Same(connections,
            Assert.IsType<Ok<List<StsDatabaseServerConnectionDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetDatabaseServerConnectionsQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetDatabaseServerConnections_ReturnsTheErrorAsAProblem()
    {
        var handler =
            HandlerMocks.Query<GetDatabaseServerConnectionsQuery, List<StsDatabaseServerConnectionDataModel>>(
                Result.Failure<List<StsDatabaseServerConnectionDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsDatabaseServerConnectionDataModel>>, ProblemHttpResult> result =
            await DatabaseServerConnectionsEndpoints.GetDatabaseServerConnections(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetDatabaseServerConnections_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetDatabaseServerConnectionsQueryHandler from GetDatabaseServerConnections",
            () => DatabaseServerConnectionsEndpoints.GetDatabaseServerConnections(HandlerMocks
                .Query<GetDatabaseServerConnectionsQuery, List<StsDatabaseServerConnectionDataModel>>(
                    new List<StsDatabaseServerConnectionDataModel>()).Object));
    }

    [Fact]
    public async Task GetDatabaseServerConnectionByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsDatabaseServerConnectionDataModel connection =
            TestData.DatabaseServerConnectionModel("Pc1.Sql", version: 4);
        var handler = GetHandler(connection);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsDatabaseServerConnectionDataModel>, ProblemHttpResult> result =
            await DatabaseServerConnectionsEndpoints.GetDatabaseServerConnectionByName("Pc1.Sql", handler.Object,
                cancellation.Token);

        Assert.Same(connection, Assert.IsType<Ok<StsDatabaseServerConnectionDataModel>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(It.Is<GetDatabaseServerConnectionByNameQuery>(q => q.Name == "Pc1.Sql"),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetDatabaseServerConnectionByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = GetHandler(Error.NotFound("RecordWithNameNotFound",
            "DatabaseServerConnection With Name Pc2.Sql Not Found"));

        Results<Ok<StsDatabaseServerConnectionDataModel>, ProblemHttpResult> result =
            await DatabaseServerConnectionsEndpoints.GetDatabaseServerConnectionByName("Pc2.Sql", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("DatabaseServerConnection With Name Pc2.Sql Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetDatabaseServerConnectionByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace(
            "Call GetDatabaseServerConnectionByNameQueryHandler for Pc1.Sql from GetDatabaseServerConnectionByName",
            () => DatabaseServerConnectionsEndpoints.GetDatabaseServerConnectionByName("Pc1.Sql",
                GetHandler(TestData.DatabaseServerConnectionModel("Pc1.Sql")).Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateDatabaseServerConnection_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsDatabaseServerConnectionDataModel connection =
            TestData.DatabaseServerConnectionModel("Other", "Pc1.WebAgent", ["Default"], 3);
        Mock<ICommandHandler<UpdateDatabaseServerConnectionCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await DatabaseServerConnectionsEndpoints.UpdateDatabaseServerConnection("Pc1.Sql", connection,
                handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateDatabaseServerConnectionCommand>(c =>
                    c.DatabaseServerConnection == connection && c.DatabaseServerConnection.Name == "Pc1.Sql" &&
                    c.DatabaseServerConnection.DbWebAgentName == "Pc1.WebAgent" &&
                    c.DatabaseServerConnection.DatabaseFoldersSets.Count == 1 &&
                    c.DatabaseServerConnection.Version == 3), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateDatabaseServerConnection_ReturnsReferencedRecordsNotFoundAsAProblem()
    {
        Mock<ICommandHandler<UpdateDatabaseServerConnectionCommand, int>> handler = UpdateHandler(
            Error.NotFound("ReferencedRecordsNotFound", "Referenced ApiClient Records Not Found: Pc2.WebAgent"));

        Results<Ok<int>, ProblemHttpResult> result =
            await DatabaseServerConnectionsEndpoints.UpdateDatabaseServerConnection("Pc1.Sql",
                TestData.DatabaseServerConnectionModel("Pc1.Sql", "Pc2.WebAgent"), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("ReferencedRecordsNotFound", problem.ProblemDetails.Title);
        Assert.Equal("Referenced ApiClient Records Not Found: Pc2.WebAgent", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateDatabaseServerConnection_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateDatabaseServerConnectionCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "DatabaseServerConnection Pc1.Sql Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result =
            await DatabaseServerConnectionsEndpoints.UpdateDatabaseServerConnection("Pc1.Sql",
                TestData.DatabaseServerConnectionModel("Pc1.Sql", version: 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("DatabaseServerConnection Pc1.Sql Version Conflict: Expected 2, Actual 3",
            problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateDatabaseServerConnection_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace(
            "Call UpdateDatabaseServerConnectionCommandHandler for Pc1.Sql from UpdateDatabaseServerConnection",
            () => DatabaseServerConnectionsEndpoints.UpdateDatabaseServerConnection("Pc1.Sql",
                TestData.DatabaseServerConnectionModel("Pc1.Sql"), UpdateHandler(1).Object));
    }

    //The body holds the user and the password, but the trace names only the record
    [Fact]
    public async Task UpdateDatabaseServerConnection_WritesNoSecretToTheDebugTrace()
    {
        IReadOnlyCollection<string> lines = await TraceOf(() =>
            DatabaseServerConnectionsEndpoints.UpdateDatabaseServerConnection("Pc1.Sql",
                TestData.DatabaseServerConnectionModel("Pc1.Sql"), UpdateHandler(1).Object));

        Assert.DoesNotContain(lines, x => x.Contains(TestData.MadeUpUser, StringComparison.Ordinal));
        Assert.DoesNotContain(lines, x => x.Contains(TestData.MadeUpPassword, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteDatabaseServerConnection_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteDatabaseServerConnectionCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await DatabaseServerConnectionsEndpoints.DeleteDatabaseServerConnection("Pc1.Sql", version,
                handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(
                It.Is<DeleteDatabaseServerConnectionCommand>(c => c.Name == "Pc1.Sql" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteDatabaseServerConnection_ReturnsConcurrencyConflictAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteDatabaseServerConnectionCommand>(Error.Conflict(
            "ConcurrencyConflict", "DatabaseServerConnection Pc1.Sql Version Conflict: Expected 1, Actual 2"));

        Results<Ok, ProblemHttpResult> result =
            await DatabaseServerConnectionsEndpoints.DeleteDatabaseServerConnection("Pc1.Sql", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteDatabaseServerConnection_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace(
            "Call DeleteDatabaseServerConnectionCommandHandler for Pc1.Sql from DeleteDatabaseServerConnection",
            () => DatabaseServerConnectionsEndpoints.DeleteDatabaseServerConnection("Pc1.Sql", null,
                HandlerMocks.Command<DeleteDatabaseServerConnectionCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("Pc1%2FSql")]
    [InlineData("Pc1%2fSql")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = GetHandler(TestData.DatabaseServerConnectionModel("Pc1/Sql"));
        Mock<ICommandHandler<UpdateDatabaseServerConnectionCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteDatabaseServerConnectionCommand>(Result.Success());
        StsDatabaseServerConnectionDataModel body = TestData.DatabaseServerConnectionModel("other");

        await DatabaseServerConnectionsEndpoints.GetDatabaseServerConnectionByName(key, get.Object);
        await DatabaseServerConnectionsEndpoints.UpdateDatabaseServerConnection(key, body, update.Object);
        await DatabaseServerConnectionsEndpoints.DeleteDatabaseServerConnection(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetDatabaseServerConnectionByNameQuery>(q => q.Name == "Pc1/Sql"),
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Pc1/Sql", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteDatabaseServerConnectionCommand>(c => c.Name == "Pc1/Sql"),
                It.IsAny<CancellationToken>()), Times.Once);
    }
}
