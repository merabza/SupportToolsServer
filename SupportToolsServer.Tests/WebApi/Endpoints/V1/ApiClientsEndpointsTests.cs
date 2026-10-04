using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.ApiClients.DeleteApiClient;
using SupportToolsServer.Application.ApiClients.GetApiClientByName;
using SupportToolsServer.Application.ApiClients.GetApiClients;
using SupportToolsServer.Application.ApiClients.UpdateApiClient;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class ApiClientsEndpointsTests
{
    private static Mock<ICommandHandler<UpdateApiClientCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateApiClientCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateApiClientCommand>(), It.IsAny<CancellationToken>()))
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
    public async Task UseApiClientsEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseApiClientsEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/apiclients/delete/{key}", "GET api/v1/apiclients", "GET api/v1/apiclients/{key}",
            "POST api/v1/apiclients/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseApiClientsEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseApiClientsEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseApiClientsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseApiClientsEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseApiClientsEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseApiClientsEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseApiClientsEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetApiClients_ReturnsTheListOfTheHandler()
    {
        List<StsApiClientDataModel> apiClients = [TestData.ApiClientModel("Pc1.WebAgent", version: 2)];
        var handler = HandlerMocks.Query<GetApiClientsQuery, List<StsApiClientDataModel>>(apiClients);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsApiClientDataModel>>, ProblemHttpResult> result =
            await ApiClientsEndpoints.GetApiClients(handler.Object, cancellation.Token);

        Assert.Same(apiClients, Assert.IsType<Ok<List<StsApiClientDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetApiClientsQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetApiClients_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetApiClientsQuery, List<StsApiClientDataModel>>(
            Result.Failure<List<StsApiClientDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsApiClientDataModel>>, ProblemHttpResult> result =
            await ApiClientsEndpoints.GetApiClients(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetApiClients_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetApiClientsQueryHandler from GetApiClients",
            () => ApiClientsEndpoints.GetApiClients(HandlerMocks
                .Query<GetApiClientsQuery, List<StsApiClientDataModel>>(new List<StsApiClientDataModel>())
                .Object));
    }

    [Fact]
    public async Task GetApiClientByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsApiClientDataModel apiClient = TestData.ApiClientModel("Pc1.WebAgent", version: 4);
        var handler = HandlerMocks.Query<GetApiClientByNameQuery, StsApiClientDataModel>(apiClient);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsApiClientDataModel>, ProblemHttpResult> result =
            await ApiClientsEndpoints.GetApiClientByName("Pc1.WebAgent", handler.Object, cancellation.Token);

        Assert.Same(apiClient, Assert.IsType<Ok<StsApiClientDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetApiClientByNameQuery>(q => q.Name == "Pc1.WebAgent"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetApiClientByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetApiClientByNameQuery, StsApiClientDataModel>(
            Error.NotFound("RecordWithNameNotFound", "ApiClient With Name Pc2.WebAgent Not Found"));

        Results<Ok<StsApiClientDataModel>, ProblemHttpResult> result =
            await ApiClientsEndpoints.GetApiClientByName("Pc2.WebAgent", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("ApiClient With Name Pc2.WebAgent Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetApiClientByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetApiClientByNameQueryHandler for Pc1.WebAgent from GetApiClientByName",
            () => ApiClientsEndpoints.GetApiClientByName("Pc1.WebAgent",
                HandlerMocks
                    .Query<GetApiClientByNameQuery, StsApiClientDataModel>(TestData.ApiClientModel("Pc1.WebAgent"))
                    .Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateApiClient_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsApiClientDataModel apiClient = TestData.ApiClientModel("Other", "http://other/", version: 3);
        Mock<ICommandHandler<UpdateApiClientCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await ApiClientsEndpoints.UpdateApiClient("Pc1.WebAgent", apiClient, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateApiClientCommand>(c =>
                    c.ApiClient == apiClient && c.ApiClient.Name == "Pc1.WebAgent" &&
                    c.ApiClient.Server == "http://other/" && c.ApiClient.ApiKey == TestData.MadeUpApiKey &&
                    c.ApiClient.Version == 3), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateApiClient_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateApiClientCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "ApiClient Pc1.WebAgent Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result = await ApiClientsEndpoints.UpdateApiClient("Pc1.WebAgent",
            TestData.ApiClientModel("Pc1.WebAgent", version: 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("ApiClient Pc1.WebAgent Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateApiClient_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateApiClientCommandHandler for Pc1.WebAgent from UpdateApiClient",
            () => ApiClientsEndpoints.UpdateApiClient("Pc1.WebAgent", TestData.ApiClientModel("Pc1.WebAgent"),
                UpdateHandler(1).Object));
    }

    //The body holds the API key, but the trace names only the record
    [Fact]
    public async Task UpdateApiClient_WritesNoSecretToTheDebugTrace()
    {
        using var trace = new CollectingTraceListener();
        Trace.Listeners.Add(trace);
        try
        {
            await ApiClientsEndpoints.UpdateApiClient("Pc1.WebAgent", TestData.ApiClientModel("Pc1.WebAgent"),
                UpdateHandler(1).Object);
        }
        finally
        {
            Trace.Listeners.Remove(trace);
        }

        Assert.DoesNotContain(trace.Lines, x => x.Contains(TestData.MadeUpApiKey, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteApiClient_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteApiClientCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await ApiClientsEndpoints.DeleteApiClient("Pc1.WebAgent", version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteApiClientCommand>(c => c.Name == "Pc1.WebAgent" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteApiClient_ReturnsConcurrencyConflictAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteApiClientCommand>(Error.Conflict("ConcurrencyConflict",
            "ApiClient Pc1.WebAgent Version Conflict: Expected 1, Actual 2"));

        Results<Ok, ProblemHttpResult> result =
            await ApiClientsEndpoints.DeleteApiClient("Pc1.WebAgent", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteApiClient_ReturnsRecordIsInUseAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteApiClientCommand>(Error.Conflict("RecordIsInUse",
            "ApiClient Pc1.WebAgent Is Used By: DatabaseServerConnection Pc1.Sql"));

        Results<Ok, ProblemHttpResult> result =
            await ApiClientsEndpoints.DeleteApiClient("Pc1.WebAgent", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("RecordIsInUse", problem.ProblemDetails.Title);
        Assert.Equal("ApiClient Pc1.WebAgent Is Used By: DatabaseServerConnection Pc1.Sql",
            problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task DeleteApiClient_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteApiClientCommandHandler for Pc1.WebAgent from DeleteApiClient",
            () => ApiClientsEndpoints.DeleteApiClient("Pc1.WebAgent", null,
                HandlerMocks.Command<DeleteApiClientCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("Pc1%2FWebAgent")]
    [InlineData("Pc1%2fWebAgent")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = HandlerMocks.Query<GetApiClientByNameQuery, StsApiClientDataModel>(
            TestData.ApiClientModel("Pc1/WebAgent"));
        Mock<ICommandHandler<UpdateApiClientCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteApiClientCommand>(Result.Success());
        StsApiClientDataModel body = TestData.ApiClientModel("other");

        await ApiClientsEndpoints.GetApiClientByName(key, get.Object);
        await ApiClientsEndpoints.UpdateApiClient(key, body, update.Object);
        await ApiClientsEndpoints.DeleteApiClient(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetApiClientByNameQuery>(q => q.Name == "Pc1/WebAgent"),
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Pc1/WebAgent", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteApiClientCommand>(c => c.Name == "Pc1/WebAgent"),
                It.IsAny<CancellationToken>()), Times.Once);
    }
}
