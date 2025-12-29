using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemToolsShared.Errors;

namespace SupportToolsServer.Api.Endpoints.V1;

// ReSharper disable once UnusedType.Global
public static class GitIgnoreFileTypes
{
    //public int InstallPriority => 50;
    //public int ServiceUsePriority => 50;

    //public bool InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args,
    //    Dictionary<string, string> parameters)
    //{
    //    return true;
    //}

    public static bool UseGitIgnoreFileTypesEndpoints(this WebApplication app, bool debugMode)
    {
        if (debugMode)
            Console.WriteLine($"{nameof(UseGitIgnoreFileTypesEndpoints)} Started");

        var group = app.MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Git.GitBase);
        //.RequireAuthorization();

        group.MapPost(SupportToolsServerApiRoutes.Git.SyncUpGitIgnoreFileTypes, SyncUpGitIgnoreFileTypes);
        //group.MapPost(SupportToolsServerApiRoutes.Git.MergeUpGitIgnoreFileTypes, MergeUpGitIgnoreFileTypes);

        if (debugMode)
            Console.WriteLine($"{nameof(UseGitIgnoreFileTypesEndpoints)} Finished");

        return true;
    }

    // POST api/v1/git/syncupgitignorefiletypes/{merge?}
    public static async Task<Results<Ok, BadRequest<Err[]>>> SyncUpGitIgnoreFileTypes([FromRoute] bool? merge,
        [FromBody] List<StsGitIgnoreFileTypeDataModel> uploadGitIgnoreFileTypes, IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(SyncUpGitIgnoreFileTypesCommandHandler)} from {nameof(SyncUpGitIgnoreFileTypes)}");

        var command = new SyncUpGitIgnoreFileTypesCommand(merge ?? false, uploadGitIgnoreFileTypes);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<Ok, BadRequest<Err[]>>>(_ => TypedResults.Ok(),
            errors => TypedResults.BadRequest(errors));
    }
}