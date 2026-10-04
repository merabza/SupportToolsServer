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
using SupportToolsServer.Application.ReactAppTemplates.DeleteReactAppTemplate;
using SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplateByName;
using SupportToolsServer.Application.ReactAppTemplates.GetReactAppTemplates;
using SupportToolsServer.Application.ReactAppTemplates.UpdateReactAppTemplate;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: React აპლიკაციების შაბლონები (CLAUDE.md, Registry conventions)
// ReSharper disable once UnusedType.Global
public static class ReactAppTemplatesEndpoints
{
    public static bool UseReactAppTemplatesEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseReactAppTemplatesEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.ReactAppTemplates.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.ReactAppTemplates.List, GetReactAppTemplates);
        group.MapGet(SupportToolsServerApiRoutes.ReactAppTemplates.ByKey, GetReactAppTemplateByName);
        group.MapPost(SupportToolsServerApiRoutes.ReactAppTemplates.Update, UpdateReactAppTemplate);
        group.MapDelete(SupportToolsServerApiRoutes.ReactAppTemplates.Delete, DeleteReactAppTemplate);

        debugLogger?.Information("{MethodName} Finished", nameof(UseReactAppTemplatesEndpoints));

        return true;
    }

    // GET api/v1/reactapptemplates
    public static async Task<Results<Ok<List<StsReactAppTemplateDataModel>>, ProblemHttpResult>> GetReactAppTemplates(
        IQueryHandler<GetReactAppTemplatesQuery, List<StsReactAppTemplateDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetReactAppTemplatesQueryHandler)} from {nameof(GetReactAppTemplates)}");

        Result<List<StsReactAppTemplateDataModel>> result =
            await handler.Handle(new GetReactAppTemplatesQuery(), cancellationToken);

        return result
            .Match<List<StsReactAppTemplateDataModel>,
                Results<Ok<List<StsReactAppTemplateDataModel>>, ProblemHttpResult>>(
                reactAppTemplates => TypedResults.Ok(reactAppTemplates),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/reactapptemplates/{key}
    public static async Task<Results<Ok<StsReactAppTemplateDataModel>, ProblemHttpResult>> GetReactAppTemplateByName(
        [FromRoute] string key, IQueryHandler<GetReactAppTemplateByNameQuery, StsReactAppTemplateDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(GetReactAppTemplateByNameQueryHandler)} for {name} from {nameof(GetReactAppTemplateByName)}");

        Result<StsReactAppTemplateDataModel> result =
            await handler.Handle(new GetReactAppTemplateByNameQuery(name), cancellationToken);

        return result.Match<StsReactAppTemplateDataModel, Results<Ok<StsReactAppTemplateDataModel>, ProblemHttpResult>>(
            reactAppTemplate => TypedResults.Ok(reactAppTemplate),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/reactapptemplates/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateReactAppTemplate([FromRoute] string key,
        [FromBody] StsReactAppTemplateDataModel reactAppTemplate,
        ICommandHandler<UpdateReactAppTemplateCommand, int> handler, CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        reactAppTemplate.Name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(UpdateReactAppTemplateCommandHandler)} for {reactAppTemplate.Name} from {nameof(UpdateReactAppTemplate)}");

        Result<int> result =
            await handler.Handle(new UpdateReactAppTemplateCommand(reactAppTemplate), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/reactapptemplates/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteReactAppTemplate([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteReactAppTemplateCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(DeleteReactAppTemplateCommandHandler)} for {name} from {nameof(DeleteReactAppTemplate)}");

        Result result = await handler.Handle(new DeleteReactAppTemplateCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
