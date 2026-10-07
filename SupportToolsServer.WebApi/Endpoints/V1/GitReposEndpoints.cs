using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Serilog;
using SupportToolsServer.Application.GitRepos.DeleteGitRepo;
using SupportToolsServer.Application.GitRepos.GetGitProjects;
using SupportToolsServer.Application.GitRepos.GetGitRepoByKey;
using SupportToolsServer.Application.GitRepos.GetGitRepos;
using SupportToolsServer.Application.GitRepos.UpdateGitRepo;
using SupportToolsServer.Application.GitRepos.UploadGitRepos;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

// ReSharper disable once UnusedType.Global
public static class GitReposEndpoints
{
    public static bool UseGitReposEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseGitReposEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Git.GitBase)
            .RequireAuthorization();

        group.MapPost(SupportToolsServerApiRoutes.Git.UploadGitRepos, UploadGitRepos);
        group.MapGet(SupportToolsServerApiRoutes.Git.GitRepos, GetGitRepos);
        group.MapGet(SupportToolsServerApiRoutes.Git.GitRepo, GetGitRepoByKey);
        group.MapPost(SupportToolsServerApiRoutes.Git.UpdateGitRepo, UpdateGitRepo);
        group.MapDelete(SupportToolsServerApiRoutes.Git.DeleteGitRepo, DeleteGitRepo);
        group.MapGet(SupportToolsServerApiRoutes.Git.GitProjects, GetGitProjects);

        debugLogger?.Information("{MethodName} Finished", nameof(UseGitReposEndpoints));

        return true;
    }

    // POST api/v1/git/uploadgitrepos
    public static async Task<Results<Ok, ProblemHttpResult>> UploadGitRepos([FromBody] SyncGitRequest syncGitRequest,
        ICommandHandler<UploadGitReposCommand> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(UploadGitReposCommandHandler)} from {nameof(UploadGitRepos)}");

        var command = new UploadGitReposCommand(syncGitRequest.Gits, syncGitRequest.GitIgnoreFiles);
        Result result = await handler.Handle(command, cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/git/gitrepos
    public static async Task<Results<Ok<List<StsGitDataModel>>, ProblemHttpResult>> GetGitRepos(
        IQueryHandler<GetGitReposQuery, List<StsGitDataModel>> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitReposQueryHandler)} from {nameof(GetGitRepos)}");

        Result<List<StsGitDataModel>> result = await handler.Handle(new GetGitReposQuery(), cancellationToken);

        return result.Match<List<StsGitDataModel>, Results<Ok<List<StsGitDataModel>>, ProblemHttpResult>>(
            gitRepos => TypedResults.Ok(gitRepos), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/git/gitrepo/{key}
    public static async Task<Results<Ok<StsGitDataModel>, ProblemHttpResult>> GetGitRepoByKey([FromRoute] string key,
        IQueryHandler<GetGitRepoByKeyQuery, StsGitDataModel> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitRepoByKeyQueryHandler)} for key {key} from {nameof(GetGitRepoByKey)}");

        Result<StsGitDataModel> result = await handler.Handle(new GetGitRepoByKeyQuery(key), cancellationToken);

        return result.Match<StsGitDataModel, Results<Ok<StsGitDataModel>, ProblemHttpResult>>(
            gitRepo => TypedResults.Ok(gitRepo), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/git/updategitrepo/{key}
    public static async Task<Results<Ok, ProblemHttpResult>> UpdateGitRepo([FromRoute] string key,
        [FromBody] StsGitDataModel gitRepo, ICommandHandler<UpdateGitRepoCommand> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(UpdateGitRepoCommandHandler)} for key {key} from {nameof(UpdateGitRepo)}");

        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        gitRepo.GitProjectName = key;

        Result result = await handler.Handle(new UpdateGitRepoCommand(gitRepo), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/git/deletegitrepo/{key}
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteGitRepo([FromRoute] string key,
        ICommandHandler<DeleteGitRepoCommand> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(DeleteGitRepoCommandHandler)} for key {key} from {nameof(DeleteGitRepo)}");

        Result result = await handler.Handle(new DeleteGitRepoCommand(key), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/git/gitprojects
    public static async Task<Results<Ok<List<StsGitProjectDataModel>>, ProblemHttpResult>> GetGitProjects(
        IQueryHandler<GetGitProjectsQuery, List<StsGitProjectDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitProjectsQueryHandler)} from {nameof(GetGitProjects)}");

        Result<List<StsGitProjectDataModel>> result =
            await handler.Handle(new GetGitProjectsQuery(), cancellationToken);

        return result.Match<List<StsGitProjectDataModel>, Results<Ok<List<StsGitProjectDataModel>>, ProblemHttpResult>>(
            gitProjects => TypedResults.Ok(gitProjects), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
