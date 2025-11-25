using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ApiKeyIdentity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerApi.Handlers;
using SupportToolsServerApi.QueryRequests;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using SupportToolsServerApiContracts.V1.Routes;
using SystemToolsShared;
using SystemToolsShared.Errors;
using WebInstallers;

namespace SupportToolsServerApi.Endpoints.V1;

// ReSharper disable once UnusedMember.Global
// ReSharper disable once UnusedType.Global
public sealed class GitEndpoints : IInstaller
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

        //git repos
        group.MapPost(SupportToolsServerApiRoutes.Git.UploadGitRepos, UploadGitRepos);
        group.MapGet(SupportToolsServerApiRoutes.Git.GitRepos, GetGitRepos);
        group.MapGet(SupportToolsServerApiRoutes.Git.GitRepo, GetOneGitRepo);
        group.MapPost(SupportToolsServerApiRoutes.Git.UpdateGitRepo, UpdateGitRepo);
        group.MapDelete(SupportToolsServerApiRoutes.Git.DeleteGitRepo, DeleteGitRepo);

        //gitIgnore FileTypes
        group.MapGet(SupportToolsServerApiRoutes.Git.GitIgnoreFileTypesList, GetGitIgnoreFileTypesList);
        group.MapPost(SupportToolsServerApiRoutes.Git.AddGitIgnoreFileTypeNameIfNotExists,
            AddGitIgnoreFileTypeNameIfNotExists);
        group.MapDelete(SupportToolsServerApiRoutes.Git.DeleteGitIgnoreFileType, DeleteGitIgnoreFileType);

        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(UseServices)} Finished");

        return true;
    }

    // POST api/git/uploadgitrepos
    public static async Task<Results<Ok, BadRequest<Err[]>>> UploadGitRepos(
        [FromBody] SyncGitRequest syncGitData, ICurrentUserByApiKey currentUserByApiKey, IMediator mediator,
        IMessagesDataManager messagesDataManager, CancellationToken cancellationToken = default)
    {
        var userName = currentUserByApiKey.Name;
        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} started", cancellationToken);
        Debug.WriteLine($"Call {nameof(UploadGitReposCommandHandler)} from {nameof(UploadGitRepos)}");

        var command =
            new UploadGitReposCommandRequest { Gits = syncGitData.Gits, GitIgnoreFiles = syncGitData.GitIgnoreFiles };
        var result = await mediator.Send(command, cancellationToken);

        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} finished", cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    // GET api/git/gitrepos
    public static async Task<Results<Ok<List<GitDataDto>>, BadRequest<Err[]>>> GetGitRepos(
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitReposQueryHandler)} from {nameof(GetGitRepos)}");

        var command = new GetGitReposQueryRequest();
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok<List<GitDataDto>>, BadRequest<Err[]>>>(res => TypedResults.Ok(res),
            errors => TypedResults.BadRequest(errors));
    }

    // GET api/git/GitRepo
    public static async Task<Results<Ok<GitDataDto>, BadRequest<Err[]>>> GetOneGitRepo(
        [FromRoute] string gitKey, IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetOneGitRepoQueryHandler)} from {nameof(GetOneGitRepo)}");

        var command = new GetOneGitRepoQueryRequest(gitKey);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok<GitDataDto>, BadRequest<Err[]>>>(res => TypedResults.Ok(res),
            errors => TypedResults.BadRequest(errors));
    }

    // POST api/git/updategitrepo/{gitKey}
    public static async Task<Results<Ok, BadRequest<Err[]>>> UpdateOneGitRepo([FromRoute] string gitKey,
        [FromBody] GitDataDto newRecord, IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(UpdateOneGitRepoCommandHandler)} for key {gitKey} from {nameof(UpdateOneGitRepo)}");

        var command = new UpdateOneGitRepoCommandRequest(gitKey, newRecord);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    // DELETE api/git/deletegitrepo/{gitKey}
    public static async Task<Results<Ok, BadRequest<Err[]>>> DeleteOneGitRepo([FromRoute] string gitKey,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(DeleteOneGitRepoCommandHandler)} for key {gitKey} from {nameof(DeleteOneGitRepo)}");

        var command = new DeleteOneGitRepoCommandRequest(gitKey);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    
    //gitIgnore FileTypes
    // GET api/git/gitignorefiletypeslist
    public static async Task<Results<Ok<List<GitDataDto>>, BadRequest<Err[]>>> GetGitIgnoreFileTypesList(
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitIgnoreFileTypesQueryHandler)} from {nameof(GetGitIgnoreFileTypesList)}");

        var command = new GetGitIgnoreFileTypesQueryRequest();
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok<List<GitDataDto>>, BadRequest<Err[]>>>(res => TypedResults.Ok(res),
            errors => TypedResults.BadRequest(errors));
    }

    // POST api/git/addgitignorefiletypenameifnotexists/{gitIgnoreFileTypeName}
    public static async Task<Results<Ok, BadRequest<Err[]>>> AddGitIgnoreFileTypeNameIfNotExists([FromRoute] string gitKey,
        [FromBody] GitDataDto newRecord, IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(AddGitIgnoreFileTypeNameIfNotExistsCommandHandler)} for key {gitKey} from {nameof(AddGitIgnoreFileTypeNameIfNotExists)}");

        var command = new AddGitIgnoreFileTypeNameIfNotExistsCommandRequest(gitKey, newRecord);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    // DELETE api/git/deletegitignorefiletype/{gitIgnoreFileTypeName}
    public static async Task<Results<Ok, BadRequest<Err[]>>> DeleteGitIgnoreFileType([FromRoute] string gitKey,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(DeleteGitIgnoreFileTypeCommandHandler)} for key {gitKey} from {nameof(DeleteGitIgnoreFileType)}");

        var command = new DeleteGitIgnoreFileTypeCommandRequest(gitKey);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }




}