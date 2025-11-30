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
using SupportToolsServerApi.Handlers.GitIgnoreFileTypes;
using SupportToolsServerApi.Handlers.GitRepos;
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
        group.MapPost(SupportToolsServerApiRoutes.Git.UpdateGitRepo, UpdateOneGitRepo);
        group.MapDelete(SupportToolsServerApiRoutes.Git.DeleteGitRepo, DeleteOneGitRepo);

        //gitIgnore FileTypes
        group.MapGet(SupportToolsServerApiRoutes.Git.GitIgnoreFileTypesList, GetGitIgnoreFileTypesList);
        group.MapPost(SupportToolsServerApiRoutes.Git.UpdateGitIgnoreFileType, UpdateGitIgnoreFileType);
        group.MapDelete(SupportToolsServerApiRoutes.Git.DeleteGitIgnoreFileType, DeleteGitIgnoreFileType);

        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(UseServices)} Finished");

        return true;
    }

    // POST api/git/uploadgitrepos
    public static async Task<Results<Ok, BadRequest<Err[]>>> UploadGitRepos([FromBody] SyncGitRequest syncGitData,
        ICurrentUserByApiKey currentUserByApiKey, IMediator mediator, IMessagesDataManager messagesDataManager,
        CancellationToken cancellationToken = default)
    {
        var userName = currentUserByApiKey.Name;
        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} started", cancellationToken);
        Debug.WriteLine($"Call {nameof(UploadGitReposCommandHandler)} from {nameof(UploadGitRepos)}");

        var command =
            new UploadGitReposRequestCommand { Gits = syncGitData.Gits, GitIgnoreFiles = syncGitData.GitIgnoreFiles };
        var result = await mediator.Send(command, cancellationToken);

        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} finished", cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    // GET api/git/gitrepos
    public static async Task<Results<Ok<List<StsGitDataModel>>, BadRequest<Err[]>>> GetGitRepos(IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitReposQueryHandler)} from {nameof(GetGitRepos)}");

        var command = new GetGitReposRequestQuery();
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok<List<StsGitDataModel>>, BadRequest<Err[]>>>(res => TypedResults.Ok(res),
            errors => TypedResults.BadRequest(errors));
    }

    // GET api/v1/git/gitrepo/{key}
    public static async Task<Results<Ok<StsGitDataModel>, BadRequest<Err[]>>> GetOneGitRepo([FromRoute] string key,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetOneGitRepoQueryHandler)} from {nameof(GetOneGitRepo)}");

        var command = new GetOneGitRepoRequestQuery(key);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok<StsGitDataModel>, BadRequest<Err[]>>>(res => TypedResults.Ok(res),
            errors => TypedResults.BadRequest(errors));
    }

    // POST api/git/updategitrepo
    public static async Task<Results<Ok, BadRequest<Err[]>>> UpdateOneGitRepo([FromBody] StsGitDataModel newRecord,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(UpdateOneGitRepoCommandHandler)} for key {newRecord.GitProjectName} from {nameof(UpdateOneGitRepo)}");

        var command = new UpdateOneGitRepoRequestCommand(newRecord);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    // DELETE api/git/deletegitrepo/{gitKey}
    public static async Task<Results<Ok, BadRequest<Err[]>>> DeleteOneGitRepo([FromRoute] string key,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(DeleteOneGitRepoCommandHandler)} for key {key} from {nameof(DeleteOneGitRepo)}");

        var command = new DeleteOneGitRepoRequestCommand(key);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    //gitIgnore FileTypes
    // GET api/git/gitignorefiletypeslist
    public static async Task<Results<Ok<List<StsGitIgnoreFileTypeDataModel>>, BadRequest<Err[]>>>
        GetGitIgnoreFileTypesList(IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitIgnoreFileTypesQueryHandler)} from {nameof(GetGitIgnoreFileTypesList)}");

        var command = new GetGitIgnoreFileTypesRequestQuery();
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok<List<StsGitIgnoreFileTypeDataModel>>, BadRequest<Err[]>>>(
            res => TypedResults.Ok(res), errors => TypedResults.BadRequest(errors));
    }

    // POST api/git/updategitignorefiletype
    public static async Task<Results<Ok, BadRequest<Err[]>>> UpdateGitIgnoreFileType(
        [FromBody] StsGitIgnoreFileTypeDataModel newRecord, IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(UpdateGitIgnoreFileTypeCommandHandler)} for {newRecord.Name} from {nameof(UpdateGitIgnoreFileType)}");

        var command = new UpdateGitIgnoreFileTypeRequestCommand(newRecord);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    // DELETE api/git/deletegitignorefiletype/{key}
    public static async Task<Results<Ok, BadRequest<Err[]>>> DeleteGitIgnoreFileType([FromRoute] string key,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(DeleteGitIgnoreFileTypeCommandHandler)} for key {key} from {nameof(DeleteGitIgnoreFileType)}");

        var command = new DeleteGitIgnoreFileTypeRequestCommand(key);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }
}