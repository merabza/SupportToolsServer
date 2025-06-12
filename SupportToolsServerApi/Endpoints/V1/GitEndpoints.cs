using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ApiKeyIdentity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerApi.Handlers;
using SupportToolsServerApi.QueryRequests;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using SupportToolsServerApiContracts.V1.Routes;
using SystemToolsShared;
using WebInstallers;

namespace SupportToolsServerApi.Endpoints.V1;

// ReSharper disable once UnusedType.Global
public class GitEndpoints : IInstaller
{
    public int InstallPriority => 50;
    public int ServiceUsePriority => 50;

    public bool InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args,
        Dictionary<string, string> parameters)
    {
        return true;
    }

    public bool UseServices(WebApplication app, bool debugMode)
    {
        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(UseServices)} Started");

        var group = app.MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Git.GitBase)
            .RequireAuthorization();

        group.MapPost(SupportToolsServerApiRoutes.Git.UploadGitRepos, UploadGitRepos);
        group.MapGet(SupportToolsServerApiRoutes.Git.GitRepos, GetGitRepos);

        //git repos
        group.MapGet(SupportToolsServerApiRoutes.Git.GitRepo, GetOneGitRepo);
        //group.MapPost(SupportToolsServerApiRoutes.Git.UpdateGitRepo, UpdateGitRepo);
        //group.MapDelete(SupportToolsServerApiRoutes.Git.DeleteGitRepo, DeleteGitRepo);

        //gitIgnore FileTypes
        //group.MapGet(SupportToolsServerApiRoutes.Git.GitIgnoreFileTypesList, GetGitIgnoreFileTypesList);
        //group.MapPost(SupportToolsServerApiRoutes.Git.AddGitIgnoreFileTypeNameIfNotExists,
        //    AddGitIgnoreFileTypeNameIfNotExists);
        //group.MapDelete(SupportToolsServerApiRoutes.Git.DeleteGitIgnoreFileType, DeleteGitIgnoreFileType);
        //group.MapDelete(ProjectsApiRoutes.Projects.RemoveProjectService, RemoveProjectService);
        //group.MapPost(ProjectsApiRoutes.Projects.StartService, StartService);
        //group.MapPost(ProjectsApiRoutes.Projects.StopService, StopService);
        //group.MapPost(ProjectsApiRoutes.Projects.Update, Update);
        //group.MapPost(ProjectsApiRoutes.Projects.UpdateService, UpdateService);
        //group.MapPost(ProjectsApiRoutes.Projects.UpdateSettings, UpdateSettings);

        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(UseServices)} Finished");

        return true;
    }

    // POST api/git/uploadgitrepos
    private static async Task<IResult> UploadGitRepos([FromBody] SyncGitRequest syncGitData,
        ICurrentUserByApiKey currentUserByApiKey, IMediator mediator, IMessagesDataManager messagesDataManager,
        CancellationToken cancellationToken = default)
    {
        var userName = currentUserByApiKey.Name;
        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} started", cancellationToken);
        Debug.WriteLine($"Call {nameof(UploadGitReposCommandHandler)} from {nameof(UploadGitRepos)}");

        var command =
            new UploadGitReposCommandRequest { Gits = syncGitData.Gits, GitIgnoreFiles = syncGitData.GitIgnoreFiles };
        var result = await mediator.Send(command, cancellationToken);

        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} finished", cancellationToken);
        return result.Match(_ => Results.Ok(), Results.BadRequest);
    }

    // GET api/git/gitrepos
    private static async Task<IResult> GetGitRepos(IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitReposQueryHandler)} from {nameof(UploadGitRepos)}");

        var command = new GetGitReposQueryRequest();
        var result = await mediator.Send(command, cancellationToken);

        return result.Match(Results.Ok, Results.BadRequest);
    }

    // GET api/git/GitRepo
    private static async Task<IResult> GetOneGitRepo([FromRoute] string gitKey, IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetOneGitRepoQueryHandler)} from {nameof(GetOneGitRepo)}");

        var command = new GetOneGitRepoQueryRequest(gitKey);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match(Results.Ok, Results.BadRequest);
    }

    //private static async Task<IResult> UpdateGitRepo(
    //    [FromRoute] string gitKey,
    //    [FromBody] GitDataDto newRecord,
    //    IMediator mediator,
    //    CancellationToken cancellationToken = default)
    //{
    //    Debug.WriteLine($"Call UpdateGitRepo for key {gitKey}");

    //    var command = new UpdateGitRepoCommandRequest(gitKey, newRecord);
    //    var result = await mediator.Send(command, cancellationToken);

    //    return result.Match(_ => Results.Ok(), Results.BadRequest);
    //}

    //private static async Task<IResult> DeleteGitRepo(
    //    [FromRoute] string gitKey,
    //    IMediator mediator,
    //    CancellationToken cancellationToken = default)
    //{
    //    Debug.WriteLine($"Call DeleteGitRepo for key {gitKey}");

    //    var command = new DeleteGitRepoCommandRequest(gitKey);
    //    var result = await mediator.Send(command, cancellationToken);

    //    return result.Match(_ => Results.Ok(), Results.BadRequest);
    //}
}