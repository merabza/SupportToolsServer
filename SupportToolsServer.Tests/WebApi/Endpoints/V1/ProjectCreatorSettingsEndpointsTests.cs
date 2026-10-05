using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.Settings.GetProjectCreatorSettings;
using SupportToolsServer.Application.Settings.UpdateProjectCreatorSettings;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class ProjectCreatorSettingsEndpointsTests
{
    private static Mock<ICommandHandler<UpdateProjectCreatorSettingsCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateProjectCreatorSettingsCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateProjectCreatorSettingsCommand>(), It.IsAny<CancellationToken>()))
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

    //The singleton has no key and no delete route
    [Fact]
    public async Task UseProjectCreatorSettingsEndpoints_MapsTheGetAndUpdateRoutes()
    {
        (bool mapped, List<string> routes) =
            await MappedRoutes.Of(app => app.UseProjectCreatorSettingsEndpoints(null));

        Assert.True(mapped);
        Assert.Equal(["GET api/v1/settings/projectcreator", "POST api/v1/settings/projectcreator/update"], routes);
    }

    [Fact]
    public async Task UseProjectCreatorSettingsEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseProjectCreatorSettingsEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseProjectCreatorSettingsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseProjectCreatorSettingsEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseProjectCreatorSettingsEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseProjectCreatorSettingsEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseProjectCreatorSettingsEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetProjectCreatorSettings_ReturnsTheContractOfTheHandler()
    {
        StsProjectCreatorSettingsDataModel projectCreatorSettings =
            TestData.ProjectCreatorSettingsModel("dl360", version: 2);
        var handler =
            HandlerMocks.Query<GetProjectCreatorSettingsQuery, StsProjectCreatorSettingsDataModel>(
                projectCreatorSettings);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsProjectCreatorSettingsDataModel>, ProblemHttpResult> result =
            await ProjectCreatorSettingsEndpoints.GetProjectCreatorSettings(handler.Object, cancellation.Token);

        Assert.Same(projectCreatorSettings,
            Assert.IsType<Ok<StsProjectCreatorSettingsDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetProjectCreatorSettingsQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetProjectCreatorSettings_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetProjectCreatorSettingsQuery, StsProjectCreatorSettingsDataModel>(
            Result.Failure<StsProjectCreatorSettingsDataModel>(Error.Failure("Db", "Database failure")));

        Results<Ok<StsProjectCreatorSettingsDataModel>, ProblemHttpResult> result =
            await ProjectCreatorSettingsEndpoints.GetProjectCreatorSettings(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetProjectCreatorSettings_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetProjectCreatorSettingsQueryHandler from GetProjectCreatorSettings",
            () => ProjectCreatorSettingsEndpoints.GetProjectCreatorSettings(HandlerMocks
                .Query<GetProjectCreatorSettingsQuery, StsProjectCreatorSettingsDataModel>(
                    new StsProjectCreatorSettingsDataModel()).Object));
    }

    [Fact]
    public async Task UpdateProjectCreatorSettings_UpsertsTheBodyAndReturnsTheNewVersion()
    {
        StsProjectCreatorSettingsDataModel projectCreatorSettings =
            TestData.ProjectCreatorSettingsModel("dl360", version: 3);
        Mock<ICommandHandler<UpdateProjectCreatorSettingsCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await ProjectCreatorSettingsEndpoints.UpdateProjectCreatorSettings(projectCreatorSettings,
                handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateProjectCreatorSettingsCommand>(c => c.ProjectCreatorSettings == projectCreatorSettings),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateProjectCreatorSettings_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateProjectCreatorSettingsCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "Settings ProjectCreator Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result =
            await ProjectCreatorSettingsEndpoints.UpdateProjectCreatorSettings(
                TestData.ProjectCreatorSettingsModel(version: 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("Settings ProjectCreator Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateProjectCreatorSettings_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace(
            "Call UpdateProjectCreatorSettingsCommandHandler from UpdateProjectCreatorSettings",
            () => ProjectCreatorSettingsEndpoints.UpdateProjectCreatorSettings(
                TestData.ProjectCreatorSettingsModel(), UpdateHandler(1).Object));
    }
}
