using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Serilog;
using SupportToolsServer.Application.GitRepos.DeleteGitRepo;
using SupportToolsServer.Application.GitRepos.GetGitProjects;
using SupportToolsServer.Application.GitRepos.GetGitRepoByKey;
using SupportToolsServer.Application.GitRepos.GetGitRepos;
using SupportToolsServer.Application.GitRepos.UpdateGitRepo;
using SupportToolsServer.Application.GitRepos.UploadGitRepos;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServer.WebApi.Endpoints.V1;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.WebApi.Endpoints.V1;

public sealed class GitReposEndpointsTests
{
    private static readonly Error DbFailure = Error.Failure("Db", "Database failure");

    [Fact]
    public async Task UseGitReposEndpoints_MapsTheFiveGitRepoRoutesAndTheGitProjects()
    {
        (bool mapped, List<string> routes) = await MappedRoutes.Of(app => app.UseGitReposEndpoints(null));

        Assert.True(mapped);
        Assert.Equal([
            "DELETE api/v1/git/deletegitrepo/{key}", "GET api/v1/git/gitprojects", "GET api/v1/git/gitrepo/{key}",
            "GET api/v1/git/gitrepos", "POST api/v1/git/updategitrepo/{key}", "POST api/v1/git/uploadgitrepos"
        ], routes);
    }

    [Fact]
    public async Task UseGitReposEndpoints_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        await MappedRoutes.Of(app => app.UseGitReposEndpoints(logger.Object));

        logger.Verify(l => l.Information("{MethodName} Started", "UseGitReposEndpoints"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "UseGitReposEndpoints"), Times.Once);
    }

    [Fact]
    public async Task UseGitReposEndpoints_RequiresAuthorizationOnEveryRoute()
    {
        (_, List<string> routes) = await MappedRoutes.Of(app => app.UseGitReposEndpoints(null));

        List<string> protectedRoutes = await MappedRoutes.RequiringAuthorization(app => app.UseGitReposEndpoints(null));

        Assert.NotEmpty(routes);
        Assert.Equal(routes, protectedRoutes);
    }

    [Fact]
    public async Task UploadGitRepos_PassesTheUploadedListsAndReturnsOk()
    {
        var request = new SyncGitRequest
        {
            Gits = [TestData.GitModel("RepoA", "CSharp")], GitIgnoreFiles = [TestData.GitIgnoreModel("CSharp")]
        };
        using var cancellation = new CancellationTokenSource();
        var handler = HandlerMocks.Command<UploadGitReposCommand>(Result.Success());

        Results<Ok, ProblemHttpResult> result =
            await GitReposEndpoints.UploadGitRepos(request, handler.Object, cancellation.Token);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(
                It.Is<UploadGitReposCommand>(c => c.Gits == request.Gits && c.GitIgnoreFiles == request.GitIgnoreFiles),
                cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UploadGitRepos_ReturnsTheErrorAsAProblem()
    {
        var request = new SyncGitRequest { Gits = [], GitIgnoreFiles = [] };
        var handler =
            HandlerMocks.Command<UploadGitReposCommand>(Error.Conflict("GitAddressIsInUse", "Address is used"));

        Results<Ok, ProblemHttpResult> result = await GitReposEndpoints.UploadGitRepos(request, handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("GitAddressIsInUse", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task GetGitRepos_ReturnsTheListOfTheHandler()
    {
        List<StsGitDataModel> gitRepos = [TestData.GitModel("RepoA", "CSharp")];
        var handler = HandlerMocks.Query<GetGitReposQuery, List<StsGitDataModel>>(gitRepos);

        Results<Ok<List<StsGitDataModel>>, ProblemHttpResult> result =
            await GitReposEndpoints.GetGitRepos(handler.Object);

        Assert.Same(gitRepos, Assert.IsType<Ok<List<StsGitDataModel>>>(result.Result).Value);
    }

    [Fact]
    public async Task GetGitRepos_ReturnsTheErrorAsAProblem()
    {
        var handler =
            HandlerMocks.Query<GetGitReposQuery, List<StsGitDataModel>>(
                Result.Failure<List<StsGitDataModel>>(DbFailure));

        Results<Ok<List<StsGitDataModel>>, ProblemHttpResult> result =
            await GitReposEndpoints.GetGitRepos(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetGitRepoByKey_AsksForTheRouteKeyAndReturnsTheGit()
    {
        StsGitDataModel gitRepo = TestData.GitModel("RepoA", "CSharp");
        var handler = HandlerMocks.Query<GetGitRepoByKeyQuery, StsGitDataModel>(gitRepo);

        Results<Ok<StsGitDataModel>, ProblemHttpResult> result =
            await GitReposEndpoints.GetGitRepoByKey("RepoA", handler.Object);

        Assert.Same(gitRepo, Assert.IsType<Ok<StsGitDataModel>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.Is<GetGitRepoByKeyQuery>(q => q.Key == "RepoA"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetGitRepoByKey_ReturnsNotFoundAsAProblem()
    {
        var handler = HandlerMocks.Query<GetGitRepoByKeyQuery, StsGitDataModel>(
            Result.Failure<StsGitDataModel>(Error.NotFound("GitWithKeyNotFound", "Git With Key RepoZ Not Found")));

        Results<Ok<StsGitDataModel>, ProblemHttpResult> result =
            await GitReposEndpoints.GetGitRepoByKey("RepoZ", handler.Object);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("GitWithKeyNotFound", problem.ProblemDetails.Title);
    }

    [Fact]
    public async Task UpdateGitRepo_TheRouteKeyWinsOverTheNameInTheBody()
    {
        StsGitDataModel gitRepo = TestData.GitModel("Other", "CSharp");
        var handler = HandlerMocks.Command<UpdateGitRepoCommand>(Result.Success());

        Results<Ok, ProblemHttpResult> result = await GitReposEndpoints.UpdateGitRepo("RepoA", gitRepo, handler.Object);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(
            h => h.Handle(It.Is<UpdateGitRepoCommand>(c => c.GitRepo == gitRepo && c.GitRepo.GitProjectName == "RepoA"),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGitRepo_ReturnsTheErrorAsAProblem()
    {
        var handler =
            HandlerMocks.Command<UpdateGitRepoCommand>(Error.Conflict("GitAddressIsInUse", "Address is used"));

        Results<Ok, ProblemHttpResult> result =
            await GitReposEndpoints.UpdateGitRepo("RepoA", TestData.GitModel("RepoA", "CSharp"), handler.Object);

        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task DeleteGitRepo_DeletesTheRouteKeyAndReturnsOk()
    {
        var handler = HandlerMocks.Command<DeleteGitRepoCommand>(Result.Success());

        Results<Ok, ProblemHttpResult> result = await GitReposEndpoints.DeleteGitRepo("RepoA", handler.Object);

        Assert.IsType<Ok>(result.Result);
        handler.Verify(h => h.Handle(It.Is<DeleteGitRepoCommand>(c => c.Key == "RepoA"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteGitRepo_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Command<DeleteGitRepoCommand>(Error.NotFound("GitWithKeyNotFound", "Not found"));

        Results<Ok, ProblemHttpResult> result = await GitReposEndpoints.DeleteGitRepo("RepoZ", handler.Object);

        Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetGitProjects_ReturnsTheListOfTheHandler()
    {
        List<StsGitProjectDataModel> gitProjects =
        [
            new()
            {
                GitName = "RepoA",
                ProjectRelativePath = @"RepoA\AppA",
                ProjectFileName = "AppA.csproj",
                DependsOnProjectNames = ["LibA"]
            }
        ];
        using var cancellation = new CancellationTokenSource();
        var handler = HandlerMocks.Query<GetGitProjectsQuery, List<StsGitProjectDataModel>>(gitProjects);

        Results<Ok<List<StsGitProjectDataModel>>, ProblemHttpResult> result =
            await GitReposEndpoints.GetGitProjects(handler.Object, cancellation.Token);

        Assert.Same(gitProjects, Assert.IsType<Ok<List<StsGitProjectDataModel>>>(result.Result).Value);
        handler.Verify(h => h.Handle(It.IsAny<GetGitProjectsQuery>(), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetGitProjects_ReturnsTheErrorAsAProblem()
    {
        var handler = HandlerMocks.Query<GetGitProjectsQuery, List<StsGitProjectDataModel>>(
            Result.Failure<List<StsGitProjectDataModel>>(DbFailure));

        Results<Ok<List<StsGitProjectDataModel>>, ProblemHttpResult> result =
            await GitReposEndpoints.GetGitProjects(handler.Object);

        Assert.Equal(StatusCodes.Status500InternalServerError,
            Assert.IsType<ProblemHttpResult>(result.Result).StatusCode);
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
            await GitReposEndpoints.UploadGitRepos(new SyncGitRequest { Gits = [], GitIgnoreFiles = [] },
                HandlerMocks.Command<UploadGitReposCommand>(DbFailure).Object);
            await GitReposEndpoints.GetGitRepos(HandlerMocks
                .Query<GetGitReposQuery, List<StsGitDataModel>>(Result.Failure<List<StsGitDataModel>>(DbFailure))
                .Object);
            await GitReposEndpoints.GetGitRepoByKey("RepoA",
                HandlerMocks.Query<GetGitRepoByKeyQuery, StsGitDataModel>(Result.Failure<StsGitDataModel>(DbFailure))
                    .Object);
            await GitReposEndpoints.UpdateGitRepo("RepoA", TestData.GitModel("RepoA", "CSharp"),
                HandlerMocks.Command<UpdateGitRepoCommand>(DbFailure).Object);
            await GitReposEndpoints.DeleteGitRepo("RepoA",
                HandlerMocks.Command<DeleteGitRepoCommand>(DbFailure).Object);
            await GitReposEndpoints.GetGitProjects(HandlerMocks
                .Query<GetGitProjectsQuery, List<StsGitProjectDataModel>>(
                    Result.Failure<List<StsGitProjectDataModel>>(DbFailure)).Object);
        }
        finally
        {
            Trace.Listeners.Remove(trace);
        }

        string[] expectedLines =
        [
            "Call UploadGitReposCommandHandler from UploadGitRepos", "Call GetGitReposQueryHandler from GetGitRepos",
            "Call GetGitRepoByKeyQueryHandler for key RepoA from GetGitRepoByKey",
            "Call UpdateGitRepoCommandHandler for key RepoA from UpdateGitRepo",
            "Call DeleteGitRepoCommandHandler for key RepoA from DeleteGitRepo",
            "Call GetGitProjectsQueryHandler from GetGitProjects"
        ];
#if DEBUG
        Assert.All(expectedLines, line => Assert.Contains(line, trace.Lines));
#else
        Assert.All(expectedLines, line => Assert.DoesNotContain(line, trace.Lines));
#endif
    }
}
