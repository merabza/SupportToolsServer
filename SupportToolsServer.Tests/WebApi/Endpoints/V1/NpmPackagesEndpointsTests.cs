using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.NpmPackages.DeleteNpmPackage;
using SupportToolsServer.Application.NpmPackages.GetNpmPackageByName;
using SupportToolsServer.Application.NpmPackages.GetNpmPackages;
using SupportToolsServer.Application.NpmPackages.UpdateNpmPackage;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class NpmPackagesEndpointsTests
{
    private static Mock<ICommandHandler<UpdateNpmPackageCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateNpmPackageCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateNpmPackageCommand>(), It.IsAny<CancellationToken>()))
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
    public async Task UseNpmPackagesEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseNpmPackagesEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/npmpackages/delete/{key}", "GET api/v1/npmpackages", "GET api/v1/npmpackages/{key}",
            "POST api/v1/npmpackages/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseNpmPackagesEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseNpmPackagesEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseNpmPackagesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseNpmPackagesEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseNpmPackagesEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseNpmPackagesEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseNpmPackagesEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetNpmPackages_ReturnsTheListOfTheHandler()
    {
        List<StsNpmPackageDataModel> npmPackages = [TestData.NpmPackageModel("react", "UI library", 2)];
        var handler = HandlerMocks.Query<GetNpmPackagesQuery, List<StsNpmPackageDataModel>>(npmPackages);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsNpmPackageDataModel>>, ProblemHttpResult> result =
            await NpmPackagesEndpoints.GetNpmPackages(handler.Object, cancellation.Token);

        Assert.Same(npmPackages, Assert.IsType<Ok<List<StsNpmPackageDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetNpmPackagesQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetNpmPackages_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetNpmPackagesQuery, List<StsNpmPackageDataModel>>(
            Result.Failure<List<StsNpmPackageDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsNpmPackageDataModel>>, ProblemHttpResult> result =
            await NpmPackagesEndpoints.GetNpmPackages(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetNpmPackages_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetNpmPackagesQueryHandler from GetNpmPackages",
            () => NpmPackagesEndpoints.GetNpmPackages(HandlerMocks
                .Query<GetNpmPackagesQuery, List<StsNpmPackageDataModel>>(new List<StsNpmPackageDataModel>()).Object));
    }

    [Fact]
    public async Task GetNpmPackageByName_ReturnsTheRecordOfTheRouteKey()
    {
        StsNpmPackageDataModel npmPackage = TestData.NpmPackageModel("react", "UI library", 4);
        var handler = HandlerMocks.Query<GetNpmPackageByNameQuery, StsNpmPackageDataModel>(npmPackage);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsNpmPackageDataModel>, ProblemHttpResult> result =
            await NpmPackagesEndpoints.GetNpmPackageByName("react", handler.Object, cancellation.Token);

        Assert.Same(npmPackage, Assert.IsType<Ok<StsNpmPackageDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetNpmPackageByNameQuery>(q => q.Name == "react"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetNpmPackageByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetNpmPackageByNameQuery, StsNpmPackageDataModel>(
            Error.NotFound("RecordWithNameNotFound", "NpmPackage With Name left-pad Not Found"));

        Results<Ok<StsNpmPackageDataModel>, ProblemHttpResult> result =
            await NpmPackagesEndpoints.GetNpmPackageByName("left-pad", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("NpmPackage With Name left-pad Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetNpmPackageByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetNpmPackageByNameQueryHandler for react from GetNpmPackageByName",
            () => NpmPackagesEndpoints.GetNpmPackageByName("react",
                HandlerMocks.Query<GetNpmPackageByNameQuery, StsNpmPackageDataModel>(TestData.NpmPackageModel("react"))
                    .Object));
    }

    //The route key wins over the name of the body, as in updategitrepo
    [Fact]
    public async Task UpdateNpmPackage_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsNpmPackageDataModel npmPackage = TestData.NpmPackageModel("Other", "UI library", 3);
        Mock<ICommandHandler<UpdateNpmPackageCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await NpmPackagesEndpoints.UpdateNpmPackage("react", npmPackage, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateNpmPackageCommand>(c =>
                    c.NpmPackage == npmPackage && c.NpmPackage.Name == "react" &&
                    c.NpmPackage.Description == "UI library" && c.NpmPackage.Version == 3), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task UpdateNpmPackage_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateNpmPackageCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", "NpmPackage react Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result =
            await NpmPackagesEndpoints.UpdateNpmPackage("react", TestData.NpmPackageModel("react", null, 2),
                handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("NpmPackage react Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateNpmPackage_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateNpmPackageCommandHandler for react from UpdateNpmPackage",
            () => NpmPackagesEndpoints.UpdateNpmPackage("react", TestData.NpmPackageModel("react"),
                UpdateHandler(1).Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteNpmPackage_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteNpmPackageCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await NpmPackagesEndpoints.DeleteNpmPackage("react", version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteNpmPackageCommand>(c => c.Name == "react" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteNpmPackage_ReturnsRecordIsInUseAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteNpmPackageCommand>(Error.Conflict("RecordIsInUse",
            "NpmPackage react Is Used By: Project AppA"));

        Results<Ok, ProblemHttpResult> result = await NpmPackagesEndpoints.DeleteNpmPackage("react", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("RecordIsInUse", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteNpmPackage_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteNpmPackageCommandHandler for react from DeleteNpmPackage",
            () => NpmPackagesEndpoints.DeleteNpmPackage("react", null,
                HandlerMocks.Command<DeleteNpmPackageCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("@scope%2Fname")]
    [InlineData("@scope%2fname")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = HandlerMocks.Query<GetNpmPackageByNameQuery, StsNpmPackageDataModel>(
            TestData.NpmPackageModel("@scope/name"));
        Mock<ICommandHandler<UpdateNpmPackageCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteNpmPackageCommand>(Result.Success());
        StsNpmPackageDataModel body = TestData.NpmPackageModel("other");

        await NpmPackagesEndpoints.GetNpmPackageByName(key, get.Object);
        await NpmPackagesEndpoints.UpdateNpmPackage(key, body, update.Object);
        await NpmPackagesEndpoints.DeleteNpmPackage(key, 1, delete.Object);

        get.Verify(
            h => h.Handle(It.Is<GetNpmPackageByNameQuery>(q => q.Name == "@scope/name"), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("@scope/name", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteNpmPackageCommand>(c => c.Name == "@scope/name"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
