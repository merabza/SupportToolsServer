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
using SupportToolsServer.Application.Servers.DeleteServer;
using SupportToolsServer.Application.Servers.GetServerByName;
using SupportToolsServer.Application.Servers.GetServers;
using SupportToolsServer.Application.Servers.UpdateServer;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: სერვერები (CLAUDE.md, Registry conventions). გადარქმევის endpoint არ არის: სახელი AppSettings-ის
//დაშიფვრის გასაღების ნაწილია, ამიტომ სერვერის გადარქმევა წაშლა და ახლის შექმნაა
// ReSharper disable once UnusedType.Global
public static class ServersEndpoints
{
    public static bool UseServersEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseServersEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Servers.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.Servers.List, GetServers);
        group.MapGet(SupportToolsServerApiRoutes.Servers.ByKey, GetServerByName);
        group.MapPost(SupportToolsServerApiRoutes.Servers.Update, UpdateServer);
        group.MapDelete(SupportToolsServerApiRoutes.Servers.Delete, DeleteServer);

        debugLogger?.Information("{MethodName} Finished", nameof(UseServersEndpoints));

        return true;
    }

    // GET api/v1/servers
    public static async Task<Results<Ok<List<StsServerDataModel>>, ProblemHttpResult>> GetServers(
        IQueryHandler<GetServersQuery, List<StsServerDataModel>> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetServersQueryHandler)} from {nameof(GetServers)}");

        Result<List<StsServerDataModel>> result = await handler.Handle(new GetServersQuery(), cancellationToken);

        return result.Match<List<StsServerDataModel>, Results<Ok<List<StsServerDataModel>>, ProblemHttpResult>>(
            servers => TypedResults.Ok(servers), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/servers/{key}
    public static async Task<Results<Ok<StsServerDataModel>, ProblemHttpResult>> GetServerByName(
        [FromRoute] string key, IQueryHandler<GetServerByNameQuery, StsServerDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(GetServerByNameQueryHandler)} for {name} from {nameof(GetServerByName)}");

        Result<StsServerDataModel> result = await handler.Handle(new GetServerByNameQuery(name), cancellationToken);

        return result.Match<StsServerDataModel, Results<Ok<StsServerDataModel>, ProblemHttpResult>>(
            server => TypedResults.Ok(server), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/servers/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateServer([FromRoute] string key,
        [FromBody] StsServerDataModel server, ICommandHandler<UpdateServerCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        server.Name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(UpdateServerCommandHandler)} for {server.Name} from {nameof(UpdateServer)}");

        Result<int> result = await handler.Handle(new UpdateServerCommand(server), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/servers/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteServer([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteServerCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(DeleteServerCommandHandler)} for {name} from {nameof(DeleteServer)}");

        Result result = await handler.Handle(new DeleteServerCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
