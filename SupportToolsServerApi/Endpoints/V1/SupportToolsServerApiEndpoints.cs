using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OneOf;
using Serilog;
using SupportToolsServerApi.CommandRequests;
using SupportToolsServerApi.Handlers.GitIgnoreFileTypes;
using SupportToolsServerApi.Handlers.GitRepos;
using SupportToolsServerApi.QueryRequests;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;
using WebSystemTools.ApiKeyIdentity;

namespace SupportToolsServerApi.Endpoints.V1;

// ReSharper disable once UnusedMember.Global
// ReSharper disable once UnusedType.Global
public static class SupportToolsServerApiEndpoints
{
    public static bool UseSupportToolsServerApiEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseSupportToolsServerApiEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Git.GitBase)
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

        debugLogger?.Information("{MethodName} Finished", nameof(UseSupportToolsServerApiEndpoints));

        return true;
    }

    // POST api/git/uploadgitrepos
    public static async Task<Results<Ok, BadRequest<Error[]>>> UploadGitRepos([FromBody] SyncGitRequest syncGitData,
        ICurrentUserByApiKey currentUserByApiKey, IMediator mediator, IMessagesDataManager messagesDataManager,
        CancellationToken cancellationToken = default)
    {
        string userName = currentUserByApiKey.Name;
        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} started", cancellationToken);
        Debug.WriteLine($"Call {nameof(UploadGitReposCommandHandler)} from {nameof(UploadGitRepos)}");

        var command =
            new UploadGitReposRequestCommand { Gits = syncGitData.Gits, GitIgnoreFiles = syncGitData.GitIgnoreFiles };
        OneOf<Unit, Error[]> result = await mediator.Send(command, cancellationToken);

        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} finished", cancellationToken);

        return result.Match<Results<Ok, BadRequest<Error[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    // GET api/git/gitrepos
    public static async Task<Results<Ok<List<StsGitDataModel>>, BadRequest<Error[]>>> GetGitRepos(IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitReposQueryHandler)} from {nameof(GetGitRepos)}");

        var command = new GetGitReposRequestQuery();
        OneOf<List<StsGitDataModel>, Error[]> result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok<List<StsGitDataModel>>, BadRequest<Error[]>>>(res => TypedResults.Ok(res),
            errors => TypedResults.BadRequest(errors));
    }

    // GET api/v1/git/gitrepo/{key}
    public static async Task<Results<Ok<StsGitDataModel>, BadRequest<Error[]>>> GetOneGitRepo([FromRoute] string key,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetOneGitRepoQueryHandler)} from {nameof(GetOneGitRepo)}");

        var command = new GetOneGitRepoRequestQuery(key);
        OneOf<StsGitDataModel, Error[]> result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok<StsGitDataModel>, BadRequest<Error[]>>>(res => TypedResults.Ok(res),
            errors => TypedResults.BadRequest(errors));
    }

    // POST api/git/updategitrepo
    public static async Task<Results<Ok, BadRequest<Error[]>>> UpdateOneGitRepo([FromBody] StsGitDataModel newRecord,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(UpdateOneGitRepoCommandHandler)} for key {newRecord.GitProjectName} from {nameof(UpdateOneGitRepo)}");

        var command = new UpdateOneGitRepoRequestCommand(newRecord);
        OneOf<Unit, Error[]> result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Error[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    // DELETE api/git/deletegitrepo/{gitKey}
    public static async Task<Results<Ok, BadRequest<Error[]>>> DeleteOneGitRepo([FromRoute] string key,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(DeleteOneGitRepoCommandHandler)} for key {key} from {nameof(DeleteOneGitRepo)}");

        var command = new DeleteOneGitRepoRequestCommand(key);
        OneOf<Unit, Error[]> result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Error[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    //gitIgnore FileTypes
    // GET api/git/gitignorefiletypeslist
    public static async Task<Results<Ok<List<StsGitIgnoreFileTypeDataModel>>, BadRequest<Error[]>>>
        GetGitIgnoreFileTypesList(IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitIgnoreFileTypesQueryHandler)} from {nameof(GetGitIgnoreFileTypesList)}");

        var command = new GetGitIgnoreFileTypesRequestQuery();
        OneOf<List<StsGitIgnoreFileTypeDataModel>, Error[]> result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok<List<StsGitIgnoreFileTypeDataModel>>, BadRequest<Error[]>>>(
            res => TypedResults.Ok(res), errors => TypedResults.BadRequest(errors));
    }

    // POST api/git/updategitignorefiletype
    public static async Task<Results<Ok, BadRequest<Error[]>>> UpdateGitIgnoreFileType(
        [FromBody] StsGitIgnoreFileTypeDataModel newRecord, IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(UpdateGitIgnoreFileTypeCommandHandler)} for {newRecord.Name} from {nameof(UpdateGitIgnoreFileType)}");

        var command = new UpdateGitIgnoreFileTypeRequestCommand(newRecord);
        OneOf<Unit, Error[]> result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Error[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }

    // DELETE api/git/deletegitignorefiletype/{key}
    public static async Task<Results<Ok, BadRequest<Error[]>>> DeleteGitIgnoreFileType([FromRoute] string key,
        IMediator mediator, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(DeleteGitIgnoreFileTypeCommandHandler)} for key {key} from {nameof(DeleteGitIgnoreFileType)}");

        var command = new DeleteGitIgnoreFileTypeRequestCommand(key);
        OneOf<Unit, Error[]> result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Error[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }
}
