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
using SupportToolsServer.Application.DatabaseServerConnections.DeleteDatabaseServerConnection;
using SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnectionByName;
using SupportToolsServer.Application.DatabaseServerConnections.GetDatabaseServerConnections;
using SupportToolsServer.Application.DatabaseServerConnections.UpdateDatabaseServerConnection;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: ბაზის სერვერებთან კავშირები, folders set-ებით (CLAUDE.md, Registry conventions). ჩანაწერში
//მომხმარებელი და პაროლია, ამიტომ ტრეისში მხოლოდ სახელი იწერება
// ReSharper disable once UnusedType.Global
public static class DatabaseServerConnectionsEndpoints
{
    public static bool UseDatabaseServerConnectionsEndpoints(this IEndpointRouteBuilder endpoints,
        ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseDatabaseServerConnectionsEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.DatabaseServerConnections.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.DatabaseServerConnections.List, GetDatabaseServerConnections);
        group.MapGet(SupportToolsServerApiRoutes.DatabaseServerConnections.ByKey, GetDatabaseServerConnectionByName);
        group.MapPost(SupportToolsServerApiRoutes.DatabaseServerConnections.Update, UpdateDatabaseServerConnection);
        group.MapDelete(SupportToolsServerApiRoutes.DatabaseServerConnections.Delete, DeleteDatabaseServerConnection);

        debugLogger?.Information("{MethodName} Finished", nameof(UseDatabaseServerConnectionsEndpoints));

        return true;
    }

    // GET api/v1/databaseserverconnections
    public static async Task<Results<Ok<List<StsDatabaseServerConnectionDataModel>>, ProblemHttpResult>>
        GetDatabaseServerConnections(
            IQueryHandler<GetDatabaseServerConnectionsQuery, List<StsDatabaseServerConnectionDataModel>> handler,
            CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(GetDatabaseServerConnectionsQueryHandler)} from {nameof(GetDatabaseServerConnections)}");

        Result<List<StsDatabaseServerConnectionDataModel>> result =
            await handler.Handle(new GetDatabaseServerConnectionsQuery(), cancellationToken);

        return result
            .Match<List<StsDatabaseServerConnectionDataModel>,
                Results<Ok<List<StsDatabaseServerConnectionDataModel>>, ProblemHttpResult>>(
                connections => TypedResults.Ok(connections),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/databaseserverconnections/{key}
    public static async Task<Results<Ok<StsDatabaseServerConnectionDataModel>, ProblemHttpResult>>
        GetDatabaseServerConnectionByName([FromRoute] string key,
            IQueryHandler<GetDatabaseServerConnectionByNameQuery, StsDatabaseServerConnectionDataModel> handler,
            CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(GetDatabaseServerConnectionByNameQueryHandler)} for {name} from {nameof(GetDatabaseServerConnectionByName)}");

        Result<StsDatabaseServerConnectionDataModel> result =
            await handler.Handle(new GetDatabaseServerConnectionByNameQuery(name), cancellationToken);

        return result
            .Match<StsDatabaseServerConnectionDataModel,
                Results<Ok<StsDatabaseServerConnectionDataModel>, ProblemHttpResult>>(
                connection => TypedResults.Ok(connection), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/databaseserverconnections/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateDatabaseServerConnection(
        [FromRoute] string key, [FromBody] StsDatabaseServerConnectionDataModel databaseServerConnection,
        ICommandHandler<UpdateDatabaseServerConnectionCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        databaseServerConnection.Name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(UpdateDatabaseServerConnectionCommandHandler)} for {databaseServerConnection.Name} from {nameof(UpdateDatabaseServerConnection)}");

        Result<int> result =
            await handler.Handle(new UpdateDatabaseServerConnectionCommand(databaseServerConnection),
                cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/databaseserverconnections/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteDatabaseServerConnection([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteDatabaseServerConnectionCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(DeleteDatabaseServerConnectionCommandHandler)} for {name} from {nameof(DeleteDatabaseServerConnection)}");

        Result result =
            await handler.Handle(new DeleteDatabaseServerConnectionCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
