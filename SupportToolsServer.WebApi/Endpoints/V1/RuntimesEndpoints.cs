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
using SupportToolsServer.Application.Runtimes.DeleteRuntime;
using SupportToolsServer.Application.Runtimes.GetRuntimeByName;
using SupportToolsServer.Application.Runtimes.GetRuntimes;
using SupportToolsServer.Application.Runtimes.UpdateRuntime;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: Runtime-ები (CLAUDE.md, Registry conventions)
// ReSharper disable once UnusedType.Global
public static class RuntimesEndpoints
{
    public static bool UseRuntimesEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseRuntimesEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Runtimes.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.Runtimes.List, GetRuntimes);
        group.MapGet(SupportToolsServerApiRoutes.Runtimes.ByKey, GetRuntimeByName);
        group.MapPost(SupportToolsServerApiRoutes.Runtimes.Update, UpdateRuntime);
        group.MapDelete(SupportToolsServerApiRoutes.Runtimes.Delete, DeleteRuntime);

        debugLogger?.Information("{MethodName} Finished", nameof(UseRuntimesEndpoints));

        return true;
    }

    // GET api/v1/runtimes
    public static async Task<Results<Ok<List<StsRuntimeDataModel>>, ProblemHttpResult>> GetRuntimes(
        IQueryHandler<GetRuntimesQuery, List<StsRuntimeDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetRuntimesQueryHandler)} from {nameof(GetRuntimes)}");

        Result<List<StsRuntimeDataModel>> result = await handler.Handle(new GetRuntimesQuery(), cancellationToken);

        return result.Match<List<StsRuntimeDataModel>, Results<Ok<List<StsRuntimeDataModel>>, ProblemHttpResult>>(
            runtimes => TypedResults.Ok(runtimes), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/runtimes/{key}
    public static async Task<Results<Ok<StsRuntimeDataModel>, ProblemHttpResult>> GetRuntimeByName(
        [FromRoute] string key, IQueryHandler<GetRuntimeByNameQuery, StsRuntimeDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(GetRuntimeByNameQueryHandler)} for {name} from {nameof(GetRuntimeByName)}");

        Result<StsRuntimeDataModel> result = await handler.Handle(new GetRuntimeByNameQuery(name), cancellationToken);

        return result.Match<StsRuntimeDataModel, Results<Ok<StsRuntimeDataModel>, ProblemHttpResult>>(
            runtime => TypedResults.Ok(runtime), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/runtimes/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateRuntime([FromRoute] string key,
        [FromBody] StsRuntimeDataModel runtime, ICommandHandler<UpdateRuntimeCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        runtime.Name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(UpdateRuntimeCommandHandler)} for {runtime.Name} from {nameof(UpdateRuntime)}");

        Result<int> result = await handler.Handle(new UpdateRuntimeCommand(runtime), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/runtimes/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteRuntime([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteRuntimeCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(DeleteRuntimeCommandHandler)} for {name} from {nameof(DeleteRuntime)}");

        Result result = await handler.Handle(new DeleteRuntimeCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
