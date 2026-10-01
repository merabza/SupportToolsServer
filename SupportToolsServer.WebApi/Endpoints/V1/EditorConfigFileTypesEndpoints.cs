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
using SupportToolsServer.Application.EditorConfigFileTypes.DeleteEditorConfigFileType;
using SupportToolsServer.Application.EditorConfigFileTypes.GetEditorConfigFileTypes;
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

        group.MapGet(SupportToolsServerApiRoutes.Git.EditorConfigFileTypesList, GetEditorConfigFileTypesList);
        group.MapPost(SupportToolsServerApiRoutes.Git.SyncUpEditorConfigFileTypes, SyncUpEditorConfigFileTypes);
        group.MapDelete(SupportToolsServerApiRoutes.Git.DeleteEditorConfigFileType, DeleteEditorConfigFileType);

        debugLogger?.Information("{MethodName} Finished", nameof(UseEditorConfigFileTypesEndpoints));

        return true;
    }

    // GET api/v1/git/editorconfigfiletypeslist
    public static async Task<Results<Ok<List<StsEditorConfigFileTypeDataModel>>, ProblemHttpResult>>
        GetEditorConfigFileTypesList(
            IQueryHandler<GetEditorConfigFileTypesQuery, List<StsEditorConfigFileTypeDataModel>> handler,
            CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(GetEditorConfigFileTypesQueryHandler)} from {nameof(GetEditorConfigFileTypesList)}");

        Result<List<StsEditorConfigFileTypeDataModel>> result =
            await handler.Handle(new GetEditorConfigFileTypesQuery(), cancellationToken);

        return result
            .Match<List<StsEditorConfigFileTypeDataModel>,
                Results<Ok<List<StsEditorConfigFileTypeDataModel>>, ProblemHttpResult>>(
                editorConfigFileTypes => TypedResults.Ok(editorConfigFileTypes),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
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

    // DELETE api/v1/git/deleteeditorconfigfiletype/{key}
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteEditorConfigFileType([FromRoute] string key,
        ICommandHandler<DeleteEditorConfigFileTypeCommand> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(DeleteEditorConfigFileTypeCommandHandler)} for {key} from {nameof(DeleteEditorConfigFileType)}");

        Result result = await handler.Handle(new DeleteEditorConfigFileTypeCommand(key), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
