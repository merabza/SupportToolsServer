using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.Projects.DeleteProject;
using SupportToolsServer.Application.Projects.GetProjectByName;
using SupportToolsServer.Application.Projects.GetProjects;
using SupportToolsServer.Application.Projects.UpdateProject;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class ProjectsEndpointsTests
{
    private static Mock<ICommandHandler<UpdateProjectCommand, int>> UpdateHandler(Result<int> result)
    {
        var handler = new Mock<ICommandHandler<UpdateProjectCommand, int>>();
        handler.Setup(h => h.Handle(It.IsAny<UpdateProjectCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return handler;
    }

    private static Mock<IQueryHandler<GetProjectByNameQuery, StsProjectDataModel>> GetHandler(
        Result<StsProjectDataModel> result)
    {
        return HandlerMocks.Query<GetProjectByNameQuery, StsProjectDataModel>(result);
    }

    private static async Task<IReadOnlyCollection<string>> TraceOf(Func<Task> call)
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
        IReadOnlyCollection<string> lines = await TraceOf(call);

#if DEBUG
        Assert.Contains(expectedLine, lines);
#else
        Assert.DoesNotContain(expectedLine, lines);
#endif
    }

    [Fact]
    public async Task UseProjectsEndpoints_MapsTheListGetUpdateAndDeleteRoutes()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseProjectsEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/projects/delete/{key}", "GET api/v1/projects", "GET api/v1/projects/{key}",
            "POST api/v1/projects/update/{key}"
        ], routes);
    }

    [Fact]
    public async Task UseProjectsEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseProjectsEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseProjectsEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseProjectsEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseProjectsEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseProjectsEndpoints(null));

        List<string> protectedRoutes = await MappedRoutes.RequiringAuthorization(app => app.UseProjectsEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task GetProjects_ReturnsTheListOfTheHandler()
    {
        List<StsProjectDataModel> projects = [TestData.ProjectModel("AppA", version: 2)];
        var handler = HandlerMocks.Query<GetProjectsQuery, List<StsProjectDataModel>>(projects);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<List<StsProjectDataModel>>, ProblemHttpResult> result =
            await ProjectsEndpoints.GetProjects(handler.Object, cancellation.Token);

        Assert.Same(projects, Assert.IsType<Ok<List<StsProjectDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetProjectsQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetProjects_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetProjectsQuery, List<StsProjectDataModel>>(
            Result.Failure<List<StsProjectDataModel>>(Error.Failure("Db", "Database failure")));

        Results<Ok<List<StsProjectDataModel>>, ProblemHttpResult> result =
            await ProjectsEndpoints.GetProjects(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetProjects_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetProjectsQueryHandler from GetProjects",
            () => ProjectsEndpoints.GetProjects(HandlerMocks
                .Query<GetProjectsQuery, List<StsProjectDataModel>>(new List<StsProjectDataModel>()).Object));
    }

    [Fact]
    public async Task GetProjectByName_ReturnsTheProjectOfTheRouteKey()
    {
        StsProjectDataModel project = TestData.ProjectModel("AppA", version: 4);
        var handler = GetHandler(project);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<StsProjectDataModel>, ProblemHttpResult> result =
            await ProjectsEndpoints.GetProjectByName("AppA", handler.Object, cancellation.Token);

        Assert.Same(project, Assert.IsType<Ok<StsProjectDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetProjectByNameQuery>(q => q.Name == "AppA"), cancellation.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetProjectByName_ReturnsRecordWithNameNotFoundAsAProblem()
    {
        var handler = GetHandler(Error.NotFound("RecordWithNameNotFound", "Project With Name AppZ Not Found"));

        Results<Ok<StsProjectDataModel>, ProblemHttpResult> result =
            await ProjectsEndpoints.GetProjectByName("AppZ", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("RecordWithNameNotFound", problem.ProblemDetails.Title);
        Assert.Equal("Project With Name AppZ Not Found", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task GetProjectByName_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call GetProjectByNameQueryHandler for AppA from GetProjectByName",
            () => ProjectsEndpoints.GetProjectByName("AppA", GetHandler(TestData.ProjectModel("AppA")).Object));
    }

    //The route key wins over the name of the body
    [Fact]
    public async Task UpdateProject_UpsertsTheBodyUnderTheRouteKeyAndReturnsTheNewVersion()
    {
        StsProjectDataModel project = TestData.ProjectModel("Other", "default", gitProjectNames: ["RepoA"],
            version: 3);
        Mock<ICommandHandler<UpdateProjectCommand, int>> handler = UpdateHandler(4);
        using var cancellation = new CancellationTokenSource();

        Results<Ok<int>, ProblemHttpResult> result =
            await ProjectsEndpoints.UpdateProject("AppA", project, handler.Object, cancellation.Token);

        Assert.Equal(4, Assert.IsType<Ok<int>>(result.Result).Value);
        handler.Verify(
            h => h.Handle(
                It.Is<UpdateProjectCommand>(c =>
                    c.Project == project && c.Project.Name == "AppA" &&
                    c.Project.EditorConfigPatternName == "default" && c.Project.GitProjectNames.Count == 1 &&
                    c.Project.Version == 3), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateProject_ReturnsReferencedRecordsNotFoundAsAProblem()
    {
        Mock<ICommandHandler<UpdateProjectCommand, int>> handler = UpdateHandler(
            Error.NotFound("ReferencedRecordsNotFound", "Referenced GitRepo Records Not Found: RepoZ"));

        Results<Ok<int>, ProblemHttpResult> result = await ProjectsEndpoints.UpdateProject("AppA",
            TestData.ProjectModel("AppA", gitProjectNames: ["RepoZ"]), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("ReferencedRecordsNotFound", problem.ProblemDetails.Title);
        Assert.Equal("Referenced GitRepo Records Not Found: RepoZ", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateProject_ReturnsConcurrencyConflictAsAProblem()
    {
        Mock<ICommandHandler<UpdateProjectCommand, int>> handler = UpdateHandler(Error.Conflict("ConcurrencyConflict",
            "Project AppA Version Conflict: Expected 2, Actual 3"));

        Results<Ok<int>, ProblemHttpResult> result = await ProjectsEndpoints.UpdateProject("AppA",
            TestData.ProjectModel("AppA", version: 2), handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
        Assert.Equal("Project AppA Version Conflict: Expected 2, Actual 3", problem.ProblemDetails.Detail);
    }

    [Fact]
    public async Task UpdateProject_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call UpdateProjectCommandHandler for AppA from UpdateProject",
            () => ProjectsEndpoints.UpdateProject("AppA", TestData.ProjectModel("AppA"), UpdateHandler(1).Object));
    }

    //The body holds the key part of the encryption key, but the trace names only the record
    [Fact]
    public async Task UpdateProject_WritesNoSecretToTheDebugTrace()
    {
        IReadOnlyCollection<string> lines = await TraceOf(() =>
            ProjectsEndpoints.UpdateProject("AppA", TestData.ProjectModel("AppA"), UpdateHandler(1).Object));

        Assert.DoesNotContain(lines, x => x.Contains(TestData.MadeUpKeyGuidPart, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task DeleteProject_DeletesTheRouteKeyWithTheQueryVersion(int? version)
    {
        var handler = HandlerMocks.Command<DeleteProjectCommand>(Result.Success());
        using var cancellation = new CancellationTokenSource();

        Results<Ok, ProblemHttpResult> result =
            await ProjectsEndpoints.DeleteProject("AppA", version, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<DeleteProjectCommand>(c => c.Name == "AppA" && c.Version == version),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteProject_ReturnsConcurrencyConflictAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteProjectCommand>(Error.Conflict("ConcurrencyConflict",
            "Project AppA Version Conflict: Expected 1, Actual 2"));

        Results<Ok, ProblemHttpResult> result = await ProjectsEndpoints.DeleteProject("AppA", 1, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("ConcurrencyConflict", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task DeleteProject_WritesTheHandlerItCallsToTheDebugTrace()
    {
        await AssertDebugTrace("Call DeleteProjectCommandHandler for AppA from DeleteProject",
            () => ProjectsEndpoints.DeleteProject("AppA", null,
                HandlerMocks.Command<DeleteProjectCommand>(Result.Success()).Object));
    }

    //ASP.NET Core leaves %2F in a route value (Host/RouteKeyTests shows it on the real pipeline), so the endpoints
    //turn it back into the / of the name
    [Theory]
    [InlineData("App%2FA")]
    [InlineData("App%2fA")]
    public async Task EveryEndpointWithAKey_GivesTheHandlerTheSlashOfTheName(string key)
    {
        var get = GetHandler(TestData.ProjectModel("App/A"));
        Mock<ICommandHandler<UpdateProjectCommand, int>> update = UpdateHandler(1);
        var delete = HandlerMocks.Command<DeleteProjectCommand>(Result.Success());
        StsProjectDataModel body = TestData.ProjectModel("other");

        await ProjectsEndpoints.GetProjectByName(key, get.Object);
        await ProjectsEndpoints.UpdateProject(key, body, update.Object);
        await ProjectsEndpoints.DeleteProject(key, 1, delete.Object);

        get.Verify(h => h.Handle(It.Is<GetProjectByNameQuery>(q => q.Name == "App/A"), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("App/A", body.Name);
        delete.Verify(
            h => h.Handle(It.Is<DeleteProjectCommand>(c => c.Name == "App/A"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
