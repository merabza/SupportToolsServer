using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.EditorConfigFileTypes.DeleteEditorConfigFileType;
using SupportToolsServer.Application.EditorConfigFileTypes.GetEditorConfigFileTypes;
using SupportToolsServer.Application.EditorConfigFileTypes.SyncUp;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class EditorConfigFileTypesEndpointsTests
{
    [Fact]
    public async Task UseEditorConfigFileTypesEndpoints_MapsTheListSyncUpAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseEditorConfigFileTypesEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/git/deleteeditorconfigfiletype/{key}", "GET api/v1/git/editorconfigfiletypeslist",
            "POST api/v1/git/syncupeditorconfigfiletypes/{merge?}"
        ], routes);
    }

    [Fact]
    public async Task UseEditorConfigFileTypesEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseEditorConfigFileTypesEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseEditorConfigFileTypesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseEditorConfigFileTypesEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseEditorConfigFileTypesEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseEditorConfigFileTypesEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseEditorConfigFileTypesEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetEditorConfigFileTypesList_ReturnsTheListOfTheHandler()
    {
        List<StsEditorConfigFileTypeDataModel> types = [TestData.EditorConfigModel("CSharp")];
        var handler = HandlerMocks.Query<GetEditorConfigFileTypesQuery, List<StsEditorConfigFileTypeDataModel>>(types);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsEditorConfigFileTypeDataModel>>, ProblemHttpResult> result =
            await EditorConfigFileTypesEndpoints.GetEditorConfigFileTypesList(handler.Object, cancellation.Token);

        Assert.Same(types, Assert.IsType<Ok<List<StsEditorConfigFileTypeDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetEditorConfigFileTypesQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetEditorConfigFileTypesList_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetEditorConfigFileTypesQuery, List<StsEditorConfigFileTypeDataModel>>(
            Result.Failure<List<StsEditorConfigFileTypeDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsEditorConfigFileTypeDataModel>>, ProblemHttpResult> result =
            await EditorConfigFileTypesEndpoints.GetEditorConfigFileTypesList(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    //Debug.WriteLine goes to the Trace listeners and Release builds leave it out, so there the line must be missing.
    //Other tests trace in parallel, so only this line is looked for
    [Fact]
    public async Task GetEditorConfigFileTypesList_WritesTheHandlerItCallsToTheDebugTrace()
    {
        List<StsEditorConfigFileTypeDataModel> types = [];
        using var trace = new CollectingTraceListener();
        Trace.Listeners.Add(trace);
        try
        {
            await EditorConfigFileTypesEndpoints.GetEditorConfigFileTypesList(HandlerMocks
                .Query<GetEditorConfigFileTypesQuery, List<StsEditorConfigFileTypeDataModel>>(types).Object);
        }
        finally
        {
            Trace.Listeners.Remove(trace);
        }

        const string expectedLine = "Call GetEditorConfigFileTypesQueryHandler from GetEditorConfigFileTypesList";
#if DEBUG
        Assert.Contains(expectedLine, trace.Lines);
#else
        Assert.DoesNotContain(expectedLine, trace.Lines);
#endif
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task SyncUpEditorConfigFileTypes_MergesOnlyWhenTheRouteAsksForIt(bool? merge, bool expectedMerge)
    {
        List<StsEditorConfigFileTypeDataModel> uploaded = [TestData.EditorConfigModel("default")];
        var handler = HandlerMocks.Command<SyncUpEditorConfigFileTypesCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await EditorConfigFileTypesEndpoints.SyncUpEditorConfigFileTypes(merge, uploaded, handler.Object,
                cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(
                It.Is<SyncUpEditorConfigFileTypesCommand>(c =>
                    c.Merge == expectedMerge && c.UploadEditorConfigFileTypes == uploaded), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task SyncUpEditorConfigFileTypes_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Command<SyncUpEditorConfigFileTypesCommand>(
            Error.Problem("ValuesNotUnique", "Name Values Are Not Unique"));

        Results<Ok, ProblemHttpResult> result =
            await EditorConfigFileTypesEndpoints.SyncUpEditorConfigFileTypes(null, [], handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("ValuesNotUnique", problem.ProblemDetails.Title);
    }

    //Debug.WriteLine goes to the Trace listeners and Release builds leave it out, so there the line must be missing.
    //Other tests trace in parallel, so only this line is looked for
    [Fact]
    public async Task SyncUpEditorConfigFileTypes_WritesTheHandlerItCallsToTheDebugTrace()
    {
        using var trace = new CollectingTraceListener();
        Trace.Listeners.Add(trace);
        try
        {
            await EditorConfigFileTypesEndpoints.SyncUpEditorConfigFileTypes(true, [],
                HandlerMocks.Command<SyncUpEditorConfigFileTypesCommand>(Result.Success()).Object);
        }
        finally
        {
            Trace.Listeners.Remove(trace);
        }

        const string expectedLine = "Call SyncUpEditorConfigFileTypesCommandHandler from SyncUpEditorConfigFileTypes";
#if DEBUG
        Assert.Contains(expectedLine, trace.Lines);
#else
        Assert.DoesNotContain(expectedLine, trace.Lines);
#endif
    }

    [Fact]
    public async Task DeleteEditorConfigFileType_DeletesTheRouteKeyAndReturnsOk()
    {
        var handler = HandlerMocks.Command<DeleteEditorConfigFileTypeCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await EditorConfigFileTypesEndpoints.DeleteEditorConfigFileType("BaGetter", handler.Object,
                cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteEditorConfigFileTypeCommand>(c => c.Name == "BaGetter"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task DeleteEditorConfigFileType_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteEditorConfigFileTypeCommand>(Error.NotFound(
            "EditorConfigFileTypeWithNameNotFound", "EditorConfig File Type With Name React Not Found"));

        Results<Ok, ProblemHttpResult> result =
            await EditorConfigFileTypesEndpoints.DeleteEditorConfigFileType("React", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("EditorConfigFileTypeWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("EditorConfig File Type With Name React Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task DeleteEditorConfigFileType_WritesTheHandlerItCallsToTheDebugTrace()
    {
        using var trace = new CollectingTraceListener();
        Trace.Listeners.Add(trace);
        try
        {
            await EditorConfigFileTypesEndpoints.DeleteEditorConfigFileType("BaGetter",
                HandlerMocks.Command<DeleteEditorConfigFileTypeCommand>(Result.Success()).Object);
        }
        finally
        {
            Trace.Listeners.Remove(trace);
        }

        const string expectedLine =
            "Call DeleteEditorConfigFileTypeCommandHandler for BaGetter from DeleteEditorConfigFileType";
#if DEBUG
        Assert.Contains(expectedLine, trace.Lines);
#else
        Assert.DoesNotContain(expectedLine, trace.Lines);
#endif
    }
}
