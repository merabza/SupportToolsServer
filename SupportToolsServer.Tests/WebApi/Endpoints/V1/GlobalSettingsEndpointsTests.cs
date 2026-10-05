using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.Settings.GetGlobalSettings;
using SupportToolsServer.Application.Settings.UpdateGlobalSettings;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class GlobalSettingsEndpointsTests
{
    private static Mock<ICommandHandler<UpdateGlobalSettingsCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateGlobalSettingsCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateGlobalSettingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return handler;
    }

    private static async Task<List<string>> CollectTrace(Func<Task> call)
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
        List<string> lines = await CollectTrace(call);

#if DEBUG
        Assert.Contains(expectedLine, lines);
#else
        Assert.DoesNotContain(expectedLine, lines);
#endif
    }

    //The singleton has no key and no delete route
    [Fact]
    public async Task UseGlobalSettingsEndpoints_MapsTheGetAndUpdateRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseGlobalSettingsEndpoints(null));

        Assert.True(mapped);
        Assert.Equal(["GET api/v1/settings/global", "POST api/v1/settings/global/update"], routes);
    }

    [Fact]
    public async Task UseGlobalSettingsEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseGlobalSettingsEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseGlobalSettingsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseGlobalSettingsEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseGlobalSettingsEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseGlobalSettingsEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseGlobalSettingsEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetGlobalSettings_ReturnsTheContractOfTheHandler()
    {
        StsGlobalSettingsDataModel globalSettings = TestData.GlobalSettingsModel("Exchange", version: 2);
        var handler = HandlerMocks.Query<GetGlobalSettingsQuery, StsGlobalSettingsDataModel>(globalSettings);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsGlobalSettingsDataModel>, ProblemHttpResult> result =
            await GlobalSettingsEndpoints.GetGlobalSettings(handler.Object, cancellation.Token);

        Assert.Same(globalSettings, Assert.IsType<Ok<StsGlobalSettingsDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetGlobalSettingsQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetGlobalSettings_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetGlobalSettingsQuery, StsGlobalSettingsDataModel>(
            Result.Failure<StsGlobalSettingsDataModel>(Error.Failure("Db", "Database failure")));

        Results<Ok<StsGlobalSettingsDataModel>, ProblemHttpResult> result =
            await GlobalSettingsEndpoints.GetGlobalSettings(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetGlobalSettings_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetGlobalSettingsQueryHandler from GetGlobalSettings",
            () => GlobalSettingsEndpoints.GetGlobalSettings(HandlerMocks
                .Query<GetGlobalSettingsQuery, StsGlobalSettingsDataModel>(new StsGlobalSettingsDataModel()).Object));
    }

    [Fact]
    public async Task UpdateGlobalSettings_UpsertsTheBodyAndReturnsTheNewVersion()
    {
        StsGlobalSettingsDataModel globalSettings = TestData.GlobalSettingsModel("Exchange", version: 3);
        Mock<ICommandHandler<UpdateGlobalSettingsCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await GlobalSettingsEndpoints.UpdateGlobalSettings(globalSettings, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<UpdateGlobalSettingsCommand>(c => c.GlobalSettings == globalSettings),
            cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateGlobalSettings_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateGlobalSettingsCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "Settings Global Version Conflict: Expected 0, Actual 1"));

        Results<Ok<int>, ProblemHttpResult> result =
            await GlobalSettingsEndpoints.UpdateGlobalSettings(TestData.GlobalSettingsModel(), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("Settings Global Version Conflict: Expected 0, Actual 1", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateGlobalSettings_ReturnsReferencedRecordsNotFoundAsAProblem()
    {
        Mock<ICommandHandler<UpdateGlobalSettingsCommand, int>> handler = UpdateHandler(Error.NotFound(
            "ReferencedRecordsNotFound", "Referenced FileStorage Records Not Found: Missing"));

        Results<Ok<int>, ProblemHttpResult> result = await GlobalSettingsEndpoints.UpdateGlobalSettings(
            TestData.GlobalSettingsModel("Missing"), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("ReferencedRecordsNotFound", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task UpdateGlobalSettings_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateGlobalSettingsCommandHandler from UpdateGlobalSettings",
            () => GlobalSettingsEndpoints.UpdateGlobalSettings(TestData.GlobalSettingsModel(),
                UpdateHandler(1).Object));
    }

    //The body holds the MediatR license key, but the trace names only the handler
    [Fact]
    public async Task UpdateGlobalSettings_WritesNoSecretToTheDebugTrace()
    {
        List<string> lines = await CollectTrace(() =>
            GlobalSettingsEndpoints.UpdateGlobalSettings(TestData.GlobalSettingsModel(), UpdateHandler(1).Object));

        Assert.DoesNotContain(lines, x => x.Contains(TestData.MadeUpLicenseKey, StringComparison.Ordinal));
    }
}
