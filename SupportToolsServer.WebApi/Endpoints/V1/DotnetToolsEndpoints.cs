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
using SupportToolsServer.Application.DotnetTools.DeleteDotnetTool;
using SupportToolsServer.Application.DotnetTools.GetDotnetToolByName;
using SupportToolsServer.Application.DotnetTools.GetDotnetTools;
using SupportToolsServer.Application.DotnetTools.UpdateDotnetTool;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: dotnet-ის ხელსაწყოები (CLAUDE.md, Registry conventions)
// ReSharper disable once UnusedType.Global
public static class DotnetToolsEndpoints
{
    public static bool UseDotnetToolsEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseDotnetToolsEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.DotnetTools.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.DotnetTools.List, GetDotnetTools);
        group.MapGet(SupportToolsServerApiRoutes.DotnetTools.ByKey, GetDotnetToolByName);
        group.MapPost(SupportToolsServerApiRoutes.DotnetTools.Update, UpdateDotnetTool);
        group.MapDelete(SupportToolsServerApiRoutes.DotnetTools.Delete, DeleteDotnetTool);

        debugLogger?.Information("{MethodName} Finished", nameof(UseDotnetToolsEndpoints));

        return true;
    }

    // GET api/v1/dotnettools
    public static async Task<Results<Ok<List<StsDotnetToolDataModel>>, ProblemHttpResult>> GetDotnetTools(
        IQueryHandler<GetDotnetToolsQuery, List<StsDotnetToolDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetDotnetToolsQueryHandler)} from {nameof(GetDotnetTools)}");

        Result<List<StsDotnetToolDataModel>> result =
            await handler.Handle(new GetDotnetToolsQuery(), cancellationToken);

        return result.Match<List<StsDotnetToolDataModel>, Results<Ok<List<StsDotnetToolDataModel>>, ProblemHttpResult>>(
            dotnetTools => TypedResults.Ok(dotnetTools), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/dotnettools/{key}
    public static async Task<Results<Ok<StsDotnetToolDataModel>, ProblemHttpResult>> GetDotnetToolByName(
        [FromRoute] string key, IQueryHandler<GetDotnetToolByNameQuery, StsDotnetToolDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(GetDotnetToolByNameQueryHandler)} for {name} from {nameof(GetDotnetToolByName)}");

        Result<StsDotnetToolDataModel> result =
            await handler.Handle(new GetDotnetToolByNameQuery(name), cancellationToken);

        return result.Match<StsDotnetToolDataModel, Results<Ok<StsDotnetToolDataModel>, ProblemHttpResult>>(
            dotnetTool => TypedResults.Ok(dotnetTool), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/dotnettools/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateDotnetTool([FromRoute] string key,
        [FromBody] StsDotnetToolDataModel dotnetTool, ICommandHandler<UpdateDotnetToolCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        dotnetTool.Name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(UpdateDotnetToolCommandHandler)} for {dotnetTool.Name} from {nameof(UpdateDotnetTool)}");

        Result<int> result = await handler.Handle(new UpdateDotnetToolCommand(dotnetTool), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/dotnettools/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteDotnetTool([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteDotnetToolCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(DeleteDotnetToolCommandHandler)} for {name} from {nameof(DeleteDotnetTool)}");

        Result result = await handler.Handle(new DeleteDotnetToolCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
