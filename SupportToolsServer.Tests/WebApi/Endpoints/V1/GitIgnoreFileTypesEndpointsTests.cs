using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.GitIgnoreFileTypes.DeleteGitIgnoreFileType;
using SupportToolsServer.Application.GitIgnoreFileTypes.EnsureGitIgnoreFileType;
using SupportToolsServer.Application.GitIgnoreFileTypes.GetGitIgnoreFileTypes;
using SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class GitIgnoreFileTypesEndpointsTests
{
    private static readonly Error DbFailure = Error.Failure("Db", "Database failure");

    [Fact]
    public async Task UseGitIgnoreFileTypesEndpoints_MapsTheFourGitIgnoreFileTypeRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseGitIgnoreFileTypesEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/git/deletegitignorefiletype/{key}", "GET api/v1/git/gitignorefiletypeslist",
            "POST api/v1/git/syncupgitignorefiletypes/{merge?}", "POST api/v1/git/updategitignorefiletype/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseGitIgnoreFileTypesEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseGitIgnoreFileTypesEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseGitIgnoreFileTypesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseGitIgnoreFileTypesEndpoints"), Times.Once);
    }

    [Fact]
    public async Task GetGitIgnoreFileTypesList_ReturnsTheListOfTheHandler()
    {
        List<StsGitIgnoreFileTypeDataModel> types = [TestData.GitIgnoreModel("CSharp")];
        var handler = HandlerMocks.Query<GetGitIgnoreFileTypesQuery, List<StsGitIgnoreFileTypeDataModel>>(types);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsGitIgnoreFileTypeDataModel>>, ProblemHttpResult> result =
            await GitIgnoreFileTypesEndpoints.GetGitIgnoreFileTypesList(handler.Object, cancellation.Token);

        Assert.Same(types, Assert.IsType<Ok<List<StsGitIgnoreFileTypeDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetGitIgnoreFileTypesQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetGitIgnoreFileTypesList_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetGitIgnoreFileTypesQuery, List<StsGitIgnoreFileTypeDataModel>>(
            Result.Failure<List<StsGitIgnoreFileTypeDataModel>>(DbFailure));

        Results<Ok<List<StsGitIgnoreFileTypeDataModel>>, ProblemHttpResult> result =
            await GitIgnoreFileTypesEndpoints.GetGitIgnoreFileTypesList(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task UpdateGitIgnoreFileType_EnsuresTheRouteKeyAndReturnsOk()
    {
        var handler = HandlerMocks.Command<EnsureGitIgnoreFileTypeCommand>(Result.Success());

        Results<Ok, ProblemHttpResult> result =
            await GitIgnoreFileTypesEndpoints.UpdateGitIgnoreFileType("Python", handler.Object);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<EnsureGitIgnoreFileTypeCommand>(c => c.Name == "Python"),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGitIgnoreFileType_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Command<EnsureGitIgnoreFileTypeCommand>(Error.Problem("NameIsRequired", "Name"));

        Results<Ok, ProblemHttpResult> result =
            await GitIgnoreFileTypesEndpoints.UpdateGitIgnoreFileType(" ", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("NameIsRequired", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteGitIgnoreFileType_DeletesTheRouteKeyAndReturnsOk()
    {
        var handler = HandlerMocks.Command<DeleteGitIgnoreFileTypeCommand>(Result.Success());

        Results<Ok, ProblemHttpResult> result =
            await GitIgnoreFileTypesEndpoints.DeleteGitIgnoreFileType("Python", handler.Object);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteGitIgnoreFileTypeCommand>(c => c.Name == "Python"),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGitIgnoreFileType_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteGitIgnoreFileTypeCommand>(Error.Conflict("GitIgnoreFileTypeIsInUse",
            "GitIgnore File Type Is Used By Gits: CSharp (RepoA)"));

        Results<Ok, ProblemHttpResult> result =
            await GitIgnoreFileTypesEndpoints.DeleteGitIgnoreFileType("CSharp", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("GitIgnore File Type Is Used By Gits: CSharp (RepoA)", problem.ProblemDetails.Detail);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task SyncUpGitIgnoreFileTypes_MergesOnlyWhenTheRouteAsksForIt(bool? merge, bool expectedMerge)
    {
        List<StsGitIgnoreFileTypeDataModel> uploaded = [TestData.GitIgnoreModel("CSharp")];
        var handler = HandlerMocks.Command<SyncUpGitIgnoreFileTypesCommand>(Result.Success());

        Results<Ok, ProblemHttpResult> result =
            await GitIgnoreFileTypesEndpoints.SyncUpGitIgnoreFileTypes(merge, uploaded, handler.Object);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(
                It.Is<SyncUpGitIgnoreFileTypesCommand>(c =>
                    c.Merge == expectedMerge && c.UploadGitIgnoreFileTypes == uploaded), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncUpGitIgnoreFileTypes_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Command<SyncUpGitIgnoreFileTypesCommand>(Error.Conflict("GitIgnoreFileTypeIsInUse",
            "GitIgnore File Type Is Used By Gits: React (RepoB)"));

        Results<Ok, ProblemHttpResult> result =
            await GitIgnoreFileTypesEndpoints.SyncUpGitIgnoreFileTypes(null, [], handler.Object);

        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    //Debug.WriteLine goes to the Trace listeners and Release builds leave it out, so there the lines must be missing.
    //Other tests trace in parallel, so only these lines are looked for
    [Fact]
    public async Task Endpoints_WriteTheHandlerTheyCallToTheDebugTrace()
    {
        using var trace = new CollectingTraceListener();
        Trace.Listeners.Add(trace);
        try
        {
            await GitIgnoreFileTypesEndpoints.GetGitIgnoreFileTypesList(HandlerMocks
                .Query<GetGitIgnoreFileTypesQuery, List<StsGitIgnoreFileTypeDataModel>>(
                    Result.Failure<List<StsGitIgnoreFileTypeDataModel>>(DbFailure)).Object);
            await GitIgnoreFileTypesEndpoints.UpdateGitIgnoreFileType("Python",
                HandlerMocks.Command<EnsureGitIgnoreFileTypeCommand>(DbFailure).Object);
            await GitIgnoreFileTypesEndpoints.DeleteGitIgnoreFileType("Python",
                HandlerMocks.Command<DeleteGitIgnoreFileTypeCommand>(DbFailure).Object);
            await GitIgnoreFileTypesEndpoints.SyncUpGitIgnoreFileTypes(true, [],
                HandlerMocks.Command<SyncUpGitIgnoreFileTypesCommand>(DbFailure).Object);
        }
        finally
        {
            Trace.Listeners.Remove(trace);
        }

        string[] expectedLines =
        [
            "Call GetGitIgnoreFileTypesQueryHandler from GetGitIgnoreFileTypesList",
            "Call EnsureGitIgnoreFileTypeCommandHandler for Python from UpdateGitIgnoreFileType",
            "Call DeleteGitIgnoreFileTypeCommandHandler for Python from DeleteGitIgnoreFileType",
            "Call SyncUpGitIgnoreFileTypesCommandHandler from SyncUpGitIgnoreFileTypes"
        ];
#if DEBUG
        Assert.All(expectedLines, line => Assert.Contains(line, trace.Lines));
#else
        Assert.All(expectedLines, line => Assert.DoesNotContain(line, trace.Lines));
#endif
    }
}
