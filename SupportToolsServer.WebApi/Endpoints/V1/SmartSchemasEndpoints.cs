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
using SupportToolsServer.Application.SmartSchemas.DeleteSmartSchema;
using SupportToolsServer.Application.SmartSchemas.GetSmartSchemaByName;
using SupportToolsServer.Application.SmartSchemas.GetSmartSchemas;
using SupportToolsServer.Application.SmartSchemas.UpdateSmartSchema;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: ჭკვიანი სქემები, დეტალებით (CLAUDE.md, Registry conventions)
// ReSharper disable once UnusedType.Global
public static class SmartSchemasEndpoints
{
    public static bool UseSmartSchemasEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseSmartSchemasEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.SmartSchemas.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.SmartSchemas.List, GetSmartSchemas);
        group.MapGet(SupportToolsServerApiRoutes.SmartSchemas.ByKey, GetSmartSchemaByName);
        group.MapPost(SupportToolsServerApiRoutes.SmartSchemas.Update, UpdateSmartSchema);
        group.MapDelete(SupportToolsServerApiRoutes.SmartSchemas.Delete, DeleteSmartSchema);

        debugLogger?.Information("{MethodName} Finished", nameof(UseSmartSchemasEndpoints));

        return true;
    }

    // GET api/v1/smartschemas
    public static async Task<Results<Ok<List<StsSmartSchemaDataModel>>, ProblemHttpResult>> GetSmartSchemas(
        IQueryHandler<GetSmartSchemasQuery, List<StsSmartSchemaDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetSmartSchemasQueryHandler)} from {nameof(GetSmartSchemas)}");

        Result<List<StsSmartSchemaDataModel>> result =
            await handler.Handle(new GetSmartSchemasQuery(), cancellationToken);

        return result
            .Match<List<StsSmartSchemaDataModel>, Results<Ok<List<StsSmartSchemaDataModel>>, ProblemHttpResult>>(
                smartSchemas => TypedResults.Ok(smartSchemas),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/smartschemas/{key}
    public static async Task<Results<Ok<StsSmartSchemaDataModel>, ProblemHttpResult>> GetSmartSchemaByName(
        [FromRoute] string key, IQueryHandler<GetSmartSchemaByNameQuery, StsSmartSchemaDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(GetSmartSchemaByNameQueryHandler)} for {name} from {nameof(GetSmartSchemaByName)}");

        Result<StsSmartSchemaDataModel> result =
            await handler.Handle(new GetSmartSchemaByNameQuery(name), cancellationToken);

        return result.Match<StsSmartSchemaDataModel, Results<Ok<StsSmartSchemaDataModel>, ProblemHttpResult>>(
            smartSchema => TypedResults.Ok(smartSchema), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/smartschemas/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateSmartSchema([FromRoute] string key,
        [FromBody] StsSmartSchemaDataModel smartSchema, ICommandHandler<UpdateSmartSchemaCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        smartSchema.Name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(UpdateSmartSchemaCommandHandler)} for {smartSchema.Name} from {nameof(UpdateSmartSchema)}");

        Result<int> result = await handler.Handle(new UpdateSmartSchemaCommand(smartSchema), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/smartschemas/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteSmartSchema([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteSmartSchemaCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(DeleteSmartSchemaCommandHandler)} for {name} from {nameof(DeleteSmartSchema)}");

        Result result = await handler.Handle(new DeleteSmartSchemaCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
