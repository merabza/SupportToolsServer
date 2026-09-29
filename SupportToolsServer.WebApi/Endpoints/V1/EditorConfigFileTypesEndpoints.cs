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
using SupportToolsServer.Application.EditorConfigFileTypes.SyncUp;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

// ReSharper disable once UnusedType.Global
public static class EditorConfigFileTypesEndpoints
{
    public static bool UseEditorConfigFileTypesEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseEditorConfigFileTypesEndpoints));

        RouteGroupBuilder group =
            endpoints.MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Git.GitBase);
        //.RequireAuthorization();

        group.MapPost(SupportToolsServerApiRoutes.Git.SyncUpEditorConfigFileTypes, SyncUpEditorConfigFileTypes);

        debugLogger?.Information("{MethodName} Finished", nameof(UseEditorConfigFileTypesEndpoints));

        return true;
    }

    // POST api/v1/git/syncupeditorconfigfiletypes/{merge?}
    public static async Task<Results<Ok, ProblemHttpResult>> SyncUpEditorConfigFileTypes([FromRoute] bool? merge,
        [FromBody] List<StsEditorConfigFileTypeDataModel> uploadEditorConfigFileTypes,
        ICommandHandler<SyncUpEditorConfigFileTypesCommand> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(SyncUpEditorConfigFileTypesCommandHandler)} from {nameof(SyncUpEditorConfigFileTypes)}");

        var command = new SyncUpEditorConfigFileTypesCommand(merge ?? false, uploadEditorConfigFileTypes);
        Result result = await handler.Handle(command, cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
