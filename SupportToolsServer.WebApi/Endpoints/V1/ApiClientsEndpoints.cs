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
using SupportToolsServer.Application.ApiClients.DeleteApiClient;
using SupportToolsServer.Application.ApiClients.GetApiClientByName;
using SupportToolsServer.Application.ApiClients.GetApiClients;
using SupportToolsServer.Application.ApiClients.UpdateApiClient;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: API კლიენტები (CLAUDE.md, Registry conventions). ჩანაწერში API key-ა, ამიტომ ტრეისში მხოლოდ სახელი
//იწერება
// ReSharper disable once UnusedType.Global
public static class ApiClientsEndpoints
{
    public static bool UseApiClientsEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseApiClientsEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.ApiClients.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.ApiClients.List, GetApiClients);
        group.MapGet(SupportToolsServerApiRoutes.ApiClients.ByKey, GetApiClientByName);
        group.MapPost(SupportToolsServerApiRoutes.ApiClients.Update, UpdateApiClient);
        group.MapDelete(SupportToolsServerApiRoutes.ApiClients.Delete, DeleteApiClient);

        debugLogger?.Information("{MethodName} Finished", nameof(UseApiClientsEndpoints));

        return true;
    }

    // GET api/v1/apiclients
    public static async Task<Results<Ok<List<StsApiClientDataModel>>, ProblemHttpResult>> GetApiClients(
        IQueryHandler<GetApiClientsQuery, List<StsApiClientDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetApiClientsQueryHandler)} from {nameof(GetApiClients)}");

        Result<List<StsApiClientDataModel>> result = await handler.Handle(new GetApiClientsQuery(), cancellationToken);

        return result
            .Match<List<StsApiClientDataModel>, Results<Ok<List<StsApiClientDataModel>>, ProblemHttpResult>>(
                apiClients => TypedResults.Ok(apiClients), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/apiclients/{key}
    public static async Task<Results<Ok<StsApiClientDataModel>, ProblemHttpResult>> GetApiClientByName(
        [FromRoute] string key, IQueryHandler<GetApiClientByNameQuery, StsApiClientDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(GetApiClientByNameQueryHandler)} for {name} from {nameof(GetApiClientByName)}");

        Result<StsApiClientDataModel> result =
            await handler.Handle(new GetApiClientByNameQuery(name), cancellationToken);

        return result.Match<StsApiClientDataModel, Results<Ok<StsApiClientDataModel>, ProblemHttpResult>>(
            apiClient => TypedResults.Ok(apiClient), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/apiclients/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateApiClient([FromRoute] string key,
        [FromBody] StsApiClientDataModel apiClient, ICommandHandler<UpdateApiClientCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        apiClient.Name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(UpdateApiClientCommandHandler)} for {apiClient.Name} from {nameof(UpdateApiClient)}");

        Result<int> result = await handler.Handle(new UpdateApiClientCommand(apiClient), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/apiclients/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა. გამოყენებულ ApiClient-ს handler-ი არ შლის (409 RecordIsInUse)
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteApiClient([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteApiClientCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(DeleteApiClientCommandHandler)} for {name} from {nameof(DeleteApiClient)}");

        Result result = await handler.Handle(new DeleteApiClientCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
