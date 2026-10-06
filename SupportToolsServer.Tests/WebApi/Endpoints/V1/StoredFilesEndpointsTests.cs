using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.StoredFiles.DeleteStoredFile;
using SupportToolsServer.Application.StoredFiles.GetStoredFileByPath;
using SupportToolsServer.Application.StoredFiles.GetStoredFiles;
using SupportToolsServer.Application.StoredFiles.UpdateStoredFile;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class StoredFilesEndpointsTests
{
    private const string FilePath = @"D:\1WorkSecurity\AppA\appsettings.json";

    private static Mock<ICommandHandler<UpdateStoredFileCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateStoredFileCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateStoredFileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return handler;
    }

    //Debug.WriteLine goes to the Trace listeners and Release builds leave it out, so there the line must be missing.
    //Other tests trace in parallel, so only this line is looked for. No line holds the made-up content of a file
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
        Assert.DoesNotContain(trace.Lines,
            line => line.Contains(TestData.MadeUpFileContent, StringComparison.Ordinal));
    }

    [Fact]
    public async Task UseStoredFilesEndpoints_MapsTheListContentUpdateAndDeleteRoutesWithoutAKey()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseStoredFilesEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/files/delete", "GET api/v1/files", "GET api/v1/files/content", "POST api/v1/files/update"
        ], routes);
    }

    [Fact]
    public async Task UseStoredFilesEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseStoredFilesEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseStoredFilesEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseStoredFilesEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseStoredFilesEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseStoredFilesEndpoints(null));

        List<string> protectedRoutes =
            await MappedRoutes.RequiringAuthorization(app => app.UseStoredFilesEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetStoredFiles_ReturnsTheListOfTheHandler()
    {
        List<StsStoredFileInfoDataModel> storedFiles =
        [
            new()
            {
                Path = FilePath,
                Sha256 = "ABC",
                Length = 3,
                UpdatedAtUtc = TestData.StoredFileTime,
                Version = 2
            }
        ];
        var handler = HandlerMocks.Query<GetStoredFilesQuery, List<StsStoredFileInfoDataModel>>(storedFiles);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsStoredFileInfoDataModel>>, ProblemHttpResult> result =
            await StoredFilesEndpoints.GetStoredFiles(handler.Object, cancellation.Token);

        Assert.Same(storedFiles, Assert.IsType<Ok<List<StsStoredFileInfoDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetStoredFilesQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetStoredFiles_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetStoredFilesQuery, List<StsStoredFileInfoDataModel>>(
            Result.Failure<List<StsStoredFileInfoDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsStoredFileInfoDataModel>>, ProblemHttpResult> result =
            await StoredFilesEndpoints.GetStoredFiles(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetStoredFiles_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetStoredFilesQueryHandler from GetStoredFiles",
            () => StoredFilesEndpoints.GetStoredFiles(HandlerMocks
                .Query<GetStoredFilesQuery, List<StsStoredFileInfoDataModel>>(new List<StsStoredFileInfoDataModel>())
                .Object));
    }

    [Fact]
    public async Task GetStoredFileByPath_ReturnsTheFileOfTheQueryPath()
    {
        StsStoredFileDataModel storedFile = TestData.StoredFileModel(FilePath, version: 4);
        var handler = HandlerMocks.Query<GetStoredFileByPathQuery, StsStoredFileDataModel>(storedFile);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsStoredFileDataModel>, ProblemHttpResult> result =
            await StoredFilesEndpoints.GetStoredFileByPath(FilePath, handler.Object, cancellation.Token);

        Assert.Same(storedFile, Assert.IsType<Ok<StsStoredFileDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetStoredFileByPathQuery>(q => q.Path == FilePath), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetStoredFileByPath_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetStoredFileByPathQuery, StsStoredFileDataModel>(
            Error.NotFound("RecordWithNameNotFound", @"StoredFile With Name D:\x.json Not Found"));

        Results<Ok<StsStoredFileDataModel>, ProblemHttpResult> result =
            await StoredFilesEndpoints.GetStoredFileByPath(@"D:\x.json", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal(@"StoredFile With Name D:\x.json Not Found", problem.ProblemDetails.Detail);
    }

    //The trace names the path; the content of the answer stays out of it
    [Fact]
    public async Task GetStoredFileByPath_WritesTheHandlerAndThePathToTheDebugTrace()
    {
        await AssertDebugTrace($"Call GetStoredFileByPathQueryHandler for {FilePath} from GetStoredFileByPath",
            () => StoredFilesEndpoints.GetStoredFileByPath(FilePath,
                HandlerMocks
                    .Query<GetStoredFileByPathQuery, StsStoredFileDataModel>(TestData.StoredFileModel(FilePath))
                    .Object));
    }

    [Fact]
    public async Task UpdateStoredFile_UpsertsTheBodyAndReturnsTheNewVersion()
    {
        StsStoredFileDataModel storedFile = TestData.StoredFileModel(FilePath, version: 3);
        Mock<ICommandHandler<UpdateStoredFileCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await StoredFilesEndpoints.UpdateStoredFile(storedFile, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateStoredFileCommand>(c =>
                    c.StoredFile == storedFile && c.StoredFile.Path == FilePath &&
                    c.StoredFile.Content == TestData.MadeUpFileContent && c.StoredFile.Version == 3),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateStoredFile_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateStoredFileCommand, int>> handler = UpdateHandler(Error.Conflict(
            "ConcurrencyConflict", $"StoredFile {FilePath} Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result =
            await StoredFilesEndpoints.UpdateStoredFile(TestData.StoredFileModel(FilePath, version: 2),
                handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal($"StoredFile {FilePath} Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    //The body holds the content of a secret file: the trace names the path only
    [Fact]
    public async Task UpdateStoredFile_WritesTheHandlerAndOnlyThePathToTheDebugTrace()
    {
        await AssertDebugTrace($"Call UpdateStoredFileCommandHandler for {FilePath} from UpdateStoredFile",
            () => StoredFilesEndpoints.UpdateStoredFile(TestData.StoredFileModel(FilePath),
                UpdateHandler(1).Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteStoredFile_DeletesTheQueryPathWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteStoredFileCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await StoredFilesEndpoints.DeleteStoredFile(FilePath, version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteStoredFileCommand>(c => c.Path == FilePath && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteStoredFile_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteStoredFileCommand>(Error.NotFound("RecordWithNameNotFound",
            @"StoredFile With Name D:\x.json Not Found"));

        Results<Ok, ProblemHttpResult> result =
            await StoredFilesEndpoints.DeleteStoredFile(@"D:\x.json", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteStoredFile_WritesTheHandlerAndThePathToTheDebugTrace()
    {
        await AssertDebugTrace($"Call DeleteStoredFileCommandHandler for {FilePath} from DeleteStoredFile",
            () => StoredFilesEndpoints.DeleteStoredFile(FilePath, null,
                HandlerMocks.Command<DeleteStoredFileCommand>(Result.Success()).Object));
    }
}
