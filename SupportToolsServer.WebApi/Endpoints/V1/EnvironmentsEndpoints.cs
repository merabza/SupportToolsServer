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
using SupportToolsServer.Application.Environments.DeleteEnvironment;
using SupportToolsServer.Application.Environments.GetEnvironmentByName;
using SupportToolsServer.Application.Environments.GetEnvironments;
using SupportToolsServer.Application.Environments.UpdateEnvironment;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: გარემოები (CLAUDE.md, Registry conventions)
// ReSharper disable once UnusedType.Global
public static class EnvironmentsEndpoints
{
    public static bool UseEnvironmentsEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseEnvironmentsEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Environments.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.Environments.List, GetEnvironments);
        group.MapGet(SupportToolsServerApiRoutes.Environments.ByKey, GetEnvironmentByName);
        group.MapPost(SupportToolsServerApiRoutes.Environments.Update, UpdateEnvironment);
        group.MapDelete(SupportToolsServerApiRoutes.Environments.Delete, DeleteEnvironment);

        debugLogger?.Information("{MethodName} Finished", nameof(UseEnvironmentsEndpoints));

        return true;
    }

    // GET api/v1/environments
    public static async Task<Results<Ok<List<StsEnvironmentDataModel>>, ProblemHttpResult>> GetEnvironments(
        IQueryHandler<GetEnvironmentsQuery, List<StsEnvironmentDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetEnvironmentsQueryHandler)} from {nameof(GetEnvironments)}");

        Result<List<StsEnvironmentDataModel>> result =
            await handler.Handle(new GetEnvironmentsQuery(), cancellationToken);

        return result
            .Match<List<StsEnvironmentDataModel>, Results<Ok<List<StsEnvironmentDataModel>>, ProblemHttpResult>>(
                environments => TypedResults.Ok(environments),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/environments/{key}
    public static async Task<Results<Ok<StsEnvironmentDataModel>, ProblemHttpResult>> GetEnvironmentByName(
        [FromRoute] string key, IQueryHandler<GetEnvironmentByNameQuery, StsEnvironmentDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(GetEnvironmentByNameQueryHandler)} for {name} from {nameof(GetEnvironmentByName)}");

        Result<StsEnvironmentDataModel> result =
            await handler.Handle(new GetEnvironmentByNameQuery(name), cancellationToken);

        return result.Match<StsEnvironmentDataModel, Results<Ok<StsEnvironmentDataModel>, ProblemHttpResult>>(
            environment => TypedResults.Ok(environment), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/environments/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateEnvironment([FromRoute] string key,
        [FromBody] StsEnvironmentDataModel environment, ICommandHandler<UpdateEnvironmentCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        environment.Name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(UpdateEnvironmentCommandHandler)} for {environment.Name} from {nameof(UpdateEnvironment)}");

        Result<int> result = await handler.Handle(new UpdateEnvironmentCommand(environment), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/environments/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteEnvironment([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteEnvironmentCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(DeleteEnvironmentCommandHandler)} for {name} from {nameof(DeleteEnvironment)}");

        Result result = await handler.Handle(new DeleteEnvironmentCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
