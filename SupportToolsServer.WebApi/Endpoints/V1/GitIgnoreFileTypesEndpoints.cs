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
using SupportToolsServer.Application.GitIgnoreFileTypes.DeleteGitIgnoreFileType;
using SupportToolsServer.Application.GitIgnoreFileTypes.EnsureGitIgnoreFileType;
using SupportToolsServer.Application.GitIgnoreFileTypes.GetGitIgnoreFileTypes;
using SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

// ReSharper disable once UnusedType.Global
public static class GitIgnoreFileTypesEndpoints
{
    //public int InstallPriority => 50;
    //public int ServiceUsePriority => 50;

    //public bool InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args,
    //    Dictionary<string, string> parameters)
    //{
    //    return true;
    //}

    public static bool UseGitIgnoreFileTypesEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseGitIgnoreFileTypesEndpoints));

        RouteGroupBuilder group =
            endpoints.MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Git.GitBase);
        //.RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.Git.GitIgnoreFileTypesList, GetGitIgnoreFileTypesList);
        group.MapPost(SupportToolsServerApiRoutes.Git.UpdateGitIgnoreFileType, UpdateGitIgnoreFileType);
        group.MapPost(SupportToolsServerApiRoutes.Git.SyncUpGitIgnoreFileTypes, SyncUpGitIgnoreFileTypes);
        //group.MapPost(SupportToolsServerApiRoutes.Git.MergeUpGitIgnoreFileTypes, MergeUpGitIgnoreFileTypes);
        group.MapDelete(SupportToolsServerApiRoutes.Git.DeleteGitIgnoreFileType, DeleteGitIgnoreFileType);

        debugLogger?.Information("{MethodName} Finished", nameof(UseGitIgnoreFileTypesEndpoints));

        return true;
    }

    // GET api/v1/git/gitignorefiletypeslist
    public static async Task<Results<Ok<List<StsGitIgnoreFileTypeDataModel>>, ProblemHttpResult>>
        GetGitIgnoreFileTypesList(IQueryHandler<GetGitIgnoreFileTypesQuery, List<StsGitIgnoreFileTypeDataModel>> handler,
            CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGitIgnoreFileTypesQueryHandler)} from {nameof(GetGitIgnoreFileTypesList)}");

        Result<List<StsGitIgnoreFileTypeDataModel>> result =
            await handler.Handle(new GetGitIgnoreFileTypesQuery(), cancellationToken);

        return result
            .Match<List<StsGitIgnoreFileTypeDataModel>,
                Results<Ok<List<StsGitIgnoreFileTypeDataModel>>, ProblemHttpResult>>(
                gitIgnoreFileTypes => TypedResults.Ok(gitIgnoreFileTypes),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/git/updategitignorefiletype/{key}
    public static async Task<Results<Ok, ProblemHttpResult>> UpdateGitIgnoreFileType([FromRoute] string key,
        ICommandHandler<EnsureGitIgnoreFileTypeCommand> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(EnsureGitIgnoreFileTypeCommandHandler)} for {key} from {nameof(UpdateGitIgnoreFileType)}");

        Result result = await handler.Handle(new EnsureGitIgnoreFileTypeCommand(key), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/git/deletegitignorefiletype/{key}
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteGitIgnoreFileType([FromRoute] string key,
        ICommandHandler<DeleteGitIgnoreFileTypeCommand> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(DeleteGitIgnoreFileTypeCommandHandler)} for {key} from {nameof(DeleteGitIgnoreFileType)}");

        Result result = await handler.Handle(new DeleteGitIgnoreFileTypeCommand(key), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/git/syncupgitignorefiletypes/{merge?}
    public static async Task<Results<Ok, ProblemHttpResult>> SyncUpGitIgnoreFileTypes([FromRoute] bool? merge,
        [FromBody] List<StsGitIgnoreFileTypeDataModel> uploadGitIgnoreFileTypes,
        ICommandHandler<SyncUpGitIgnoreFileTypesCommand> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(SyncUpGitIgnoreFileTypesCommandHandler)} from {nameof(SyncUpGitIgnoreFileTypes)}");

        var command = new SyncUpGitIgnoreFileTypesCommand(merge ?? false, uploadGitIgnoreFileTypes);
        Result result = await handler.Handle(command, cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
