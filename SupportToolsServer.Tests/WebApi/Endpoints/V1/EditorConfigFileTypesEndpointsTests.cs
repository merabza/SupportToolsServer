using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
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
    public async Task UseEditorConfigFileTypesEndpoints_MapsTheSyncUpRoute()
    {
        (bool mapped, List<string> routes) =
            await MappedRoutes.Of(app => app.UseEditorConfigFileTypesEndpoints(null));

        Assert.True(mapped);
        Assert.Equal(["POST api/v1/git/syncupeditorconfigfiletypes/{merge?}"], routes);
    }

    [Fact]
    public async Task UseEditorConfigFileTypesEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseEditorConfigFileTypesEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseEditorConfigFileTypesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseEditorConfigFileTypesEndpoints"), Times.Once);
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
        handler.Verify(h => h.Handle(
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

    //Debug.WriteLine goes to the Trace listeners and Release builds leave it out. Other tests trace in parallel,
    //so only this line is looked for
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

#if DEBUG
        Assert.Contains("Call SyncUpEditorConfigFileTypesCommandHandler from SyncUpEditorConfigFileTypes",
            trace.Lines);
#endif
    }
}
