using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.FileStorages.DeleteFileStorage;
using SupportToolsServer.Application.FileStorages.GetFileStorageByName;
using SupportToolsServer.Application.FileStorages.GetFileStorages;
using SupportToolsServer.Application.FileStorages.UpdateFileStorage;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class FileStoragesEndpointsTests
{
    private static Mock<ICommandHandler<UpdateFileStorageCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateFileStorageCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateFileStorageCommand>(), It.IsAny<CancellationToken>()))
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
    public async Task UseFileStoragesEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseFileStoragesEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/filestorages/delete/{key}", "GET api/v1/filestorages", "GET api/v1/filestorages/{key}",
            "POST api/v1/filestorages/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseFileStoragesEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseFileStoragesEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseFileStoragesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseFileStoragesEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseFileStoragesEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseFileStoragesEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseFileStoragesEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetFileStorages_ReturnsTheListOfTheHandler()
    {
        List<StsFileStorageDataModel> fileStorages = [TestData.FileStorageModel("Exchange", version: 2)];
        var handler = HandlerMocks.Query<GetFileStoragesQuery, List<StsFileStorageDataModel>>(fileStorages);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsFileStorageDataModel>>, ProblemHttpResult> result =
            await FileStoragesEndpoints.GetFileStorages(handler.Object, cancellation.Token);

        Assert.Same(fileStorages, Assert.IsType<Ok<List<StsFileStorageDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetFileStoragesQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetFileStorages_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetFileStoragesQuery, List<StsFileStorageDataModel>>(
            Result.Failure<List<StsFileStorageDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsFileStorageDataModel>>, ProblemHttpResult> result =
            await FileStoragesEndpoints.GetFileStorages(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetFileStorages_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetFileStoragesQueryHandler from GetFileStorages",
            () => FileStoragesEndpoints.GetFileStorages(HandlerMocks
                .Query<GetFileStoragesQuery, List<StsFileStorageDataModel>>(new List<StsFileStorageDataModel>())
                .Object));
    }

    [Fact]
    public async Task GetFileStorageByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsFileStorageDataModel fileStorage = TestData.FileStorageModel("Exchange", version: 4);
        var handler = HandlerMocks.Query<GetFileStorageByNameQuery, StsFileStorageDataModel>(fileStorage);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsFileStorageDataModel>, ProblemHttpResult> result =
            await FileStoragesEndpoints.GetFileStorageByName("Exchange", handler.Object, cancellation.Token);

        Assert.Same(fileStorage, Assert.IsType<Ok<StsFileStorageDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetFileStorageByNameQuery>(q => q.Name == "Exchange"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetFileStorageByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetFileStorageByNameQuery, StsFileStorageDataModel>(
            Error.NotFound("RecordWithNameNotFound", "FileStorage With Name LocalBak Not Found"));

        Results<Ok<StsFileStorageDataModel>, ProblemHttpResult> result =
            await FileStoragesEndpoints.GetFileStorageByName("LocalBak", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("FileStorage With Name LocalBak Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetFileStorageByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetFileStorageByNameQueryHandler for Exchange from GetFileStorageByName",
            () => FileStoragesEndpoints.GetFileStorageByName("Exchange",
                HandlerMocks
                    .Query<GetFileStorageByNameQuery, StsFileStorageDataModel>(TestData.FileStorageModel("Exchange"))
                    .Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateFileStorage_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsFileStorageDataModel fileStorage = TestData.FileStorageModel("Other", @"D:Bak", version: 3);
        Mock<ICommandHandler<UpdateFileStorageCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await FileStoragesEndpoints.UpdateFileStorage("Exchange", fileStorage, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateFileStorageCommand>(c =>
                    c.FileStorage == fileStorage && c.FileStorage.Name == "Exchange" &&
                    c.FileStorage.FileStoragePath == @"D:Bak" && c.FileStorage.Password == TestData.MadeUpPassword &&
                    c.FileStorage.Version == 3), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateFileStorage_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateFileStorageCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "FileStorage Exchange Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result = await FileStoragesEndpoints.UpdateFileStorage("Exchange",
            TestData.FileStorageModel("Exchange", version: 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("FileStorage Exchange Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateFileStorage_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateFileStorageCommandHandler for Exchange from UpdateFileStorage",
            () => FileStoragesEndpoints.UpdateFileStorage("Exchange", TestData.FileStorageModel("Exchange"),
                UpdateHandler(1).Object));
    }

    //The body holds the user and the password, but the trace names only the record
    [Fact]
    public async Task UpdateFileStorage_WritesNoSecretToTheDebugTrace()
    {
        using var trace = new CollectingTraceListener();
        Trace.Listeners.Add(trace);
        try
        {
            await FileStoragesEndpoints.UpdateFileStorage("Exchange", TestData.FileStorageModel("Exchange"),
                UpdateHandler(1).Object);
        }
        finally
        {
            Trace.Listeners.Remove(trace);
        }

        Assert.DoesNotContain(trace.Lines, x => x.Contains(TestData.MadeUpUser, StringComparison.Ordinal));
        Assert.DoesNotContain(trace.Lines, x => x.Contains(TestData.MadeUpPassword, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteFileStorage_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteFileStorageCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await FileStoragesEndpoints.DeleteFileStorage("Exchange", version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteFileStorageCommand>(c => c.Name == "Exchange" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteFileStorage_ReturnsConcurrencyConflictAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteFileStorageCommand>(Error.Conflict("ConcurrencyConflict",
            "FileStorage Exchange Version Conflict: Expected 1, Actual 2"));

        Results<Ok, ProblemHttpResult> result =
            await FileStoragesEndpoints.DeleteFileStorage("Exchange", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteFileStorage_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteFileStorageCommandHandler for Exchange from DeleteFileStorage",
            () => FileStoragesEndpoints.DeleteFileStorage("Exchange", null,
                HandlerMocks.Command<DeleteFileStorageCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("Local%2FBak")]
    [InlineData("Local%2fBak")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = HandlerMocks.Query<GetFileStorageByNameQuery, StsFileStorageDataModel>(
            TestData.FileStorageModel("Local/Bak"));
        Mock<ICommandHandler<UpdateFileStorageCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteFileStorageCommand>(Result.Success());
        StsFileStorageDataModel body = TestData.FileStorageModel("other");

        await FileStoragesEndpoints.GetFileStorageByName(key, get.Object);
        await FileStoragesEndpoints.UpdateFileStorage(key, body, update.Object);
        await FileStoragesEndpoints.DeleteFileStorage(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetFileStorageByNameQuery>(q => q.Name == "Local/Bak"),
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Local/Bak", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteFileStorageCommand>(c => c.Name == "Local/Bak"),
                It.IsAny<CancellationToken>()), Times.Once);
    }
}
