using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.Servers.DeleteServer;
using SupportToolsServer.Application.Servers.GetServerByName;
using SupportToolsServer.Application.Servers.GetServers;
using SupportToolsServer.Application.Servers.UpdateServer;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class ServersEndpointsTests
{
    private static Mock<ICommandHandler<UpdateServerCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateServerCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateServerCommand>(), It.IsAny<CancellationToken>()))
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

    //There is no rename route: renaming a server is a delete and a create
    [Fact]
    public async Task UseServersEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseServersEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/servers/delete/{key}", "GET api/v1/servers", "GET api/v1/servers/{key}",
            "POST api/v1/servers/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseServersEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseServersEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseServersEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseServersEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseServersEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseServersEndpoints(null));

        List<string> protectedRoutes = await MappedRoutes.RequiringAuthorization(app => app.UseServersEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetServers_ReturnsTheListOfTheHandler()
    {
        List<StsServerDataModel> servers = [TestData.ServerModel("dl360", runtime: "linux-x64", version: 2)];
        var handler = HandlerMocks.Query<GetServersQuery, List<StsServerDataModel>>(servers);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsServerDataModel>>, ProblemHttpResult> result =
            await ServersEndpoints.GetServers(handler.Object, cancellation.Token);

        Assert.Same(servers, Assert.IsType<Ok<List<StsServerDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetServersQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetServers_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetServersQuery, List<StsServerDataModel>>(
            Result.Failure<List<StsServerDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsServerDataModel>>, ProblemHttpResult> result =
            await ServersEndpoints.GetServers(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetServers_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetServersQueryHandler from GetServers",
            () => ServersEndpoints.GetServers(HandlerMocks
                .Query<GetServersQuery, List<StsServerDataModel>>(new List<StsServerDataModel>()).Object));
    }

    [Fact]
    public async Task GetServerByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsServerDataModel server = TestData.ServerModel("dl360", version: 4);
        var handler = HandlerMocks.Query<GetServerByNameQuery, StsServerDataModel>(server);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsServerDataModel>, ProblemHttpResult> result =
            await ServersEndpoints.GetServerByName("dl360", handler.Object, cancellation.Token);

        Assert.Same(server, Assert.IsType<Ok<StsServerDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetServerByNameQuery>(q => q.Name == "dl360"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetServerByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetServerByNameQuery, StsServerDataModel>(
            Error.NotFound("RecordWithNameNotFound", "Server With Name guria Not Found"));

        Results<Ok<StsServerDataModel>, ProblemHttpResult> result =
            await ServersEndpoints.GetServerByName("guria", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("Server With Name guria Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetServerByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetServerByNameQueryHandler for dl360 from GetServerByName",
            () => ServersEndpoints.GetServerByName("dl360",
                HandlerMocks.Query<GetServerByNameQuery, StsServerDataModel>(TestData.ServerModel("dl360")).Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateServer_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsServerDataModel server = TestData.ServerModel("Other", "Dl360.WebAgent", null, "linux-x64", 3);
        Mock<ICommandHandler<UpdateServerCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await ServersEndpoints.UpdateServer("dl360", server, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateServerCommand>(c =>
                    c.Server == server && c.Server.Name == "dl360" && c.Server.WebAgentName == "Dl360.WebAgent" &&
                    c.Server.Runtime == "linux-x64" && c.Server.Version == 3), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateServer_ReturnsReferencedRecordsNotFoundAsAProblem()
    {
        Mock<ICommandHandler<UpdateServerCommand, int>> handler = UpdateHandler(Error.NotFound(
            "ReferencedRecordsNotFound",
            "Referenced ApiClient Records Not Found: Pc9.WebAgent; Referenced Runtime Records Not Found: osx-arm64"));

        Results<Ok<int>, ProblemHttpResult> result = await ServersEndpoints.UpdateServer("dl360",
            TestData.ServerModel("dl360", "Pc9.WebAgent", null, "osx-arm64"), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("ReferencedRecordsNotFound", problem.ProblemDetails.Title);
        Assert.Equal(
            "Referenced ApiClient Records Not Found: Pc9.WebAgent; Referenced Runtime Records Not Found: osx-arm64",
            problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateServer_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateServerCommand, int>> handler = UpdateHandler(Error.Conflict("ConcurrencyConflict",
            "Server dl360 Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result = await ServersEndpoints.UpdateServer("dl360",
            TestData.ServerModel("dl360", version: 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("Server dl360 Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateServer_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateServerCommandHandler for dl360 from UpdateServer",
            () => ServersEndpoints.UpdateServer("dl360", TestData.ServerModel("dl360"), UpdateHandler(1).Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteServer_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteServerCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await ServersEndpoints.DeleteServer("dl360", version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteServerCommand>(c => c.Name == "dl360" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteServer_ReturnsConcurrencyConflictAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteServerCommand>(Error.Conflict("ConcurrencyConflict",
            "Server dl360 Version Conflict: Expected 1, Actual 2"));

        Results<Ok, ProblemHttpResult> result = await ServersEndpoints.DeleteServer("dl360", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteServer_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteServerCommandHandler for dl360 from DeleteServer",
            () => ServersEndpoints.DeleteServer("dl360", null,
                HandlerMocks.Command<DeleteServerCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("dl360%2FOld")]
    [InlineData("dl360%2fOld")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = HandlerMocks.Query<GetServerByNameQuery, StsServerDataModel>(TestData.ServerModel("dl360/Old"));
        Mock<ICommandHandler<UpdateServerCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteServerCommand>(Result.Success());
        StsServerDataModel body = TestData.ServerModel("other");

        await ServersEndpoints.GetServerByName(key, get.Object);
        await ServersEndpoints.UpdateServer(key, body, update.Object);
        await ServersEndpoints.DeleteServer(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetServerByNameQuery>(q => q.Name == "dl360/Old"), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("dl360/Old", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteServerCommand>(c => c.Name == "dl360/Old"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
