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
using SupportToolsServer.Application.ProjectTemplates.DeleteProjectTemplate;
using SupportToolsServer.Application.ProjectTemplates.GetProjectTemplateByName;
using SupportToolsServer.Application.ProjectTemplates.GetProjectTemplates;
using SupportToolsServer.Application.ProjectTemplates.UpdateProjectTemplate;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: პროექტის შაბლონები (CLAUDE.md, Registry conventions)
// ReSharper disable once UnusedType.Global
public static class ProjectTemplatesEndpoints
{
    public static bool UseProjectTemplatesEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseProjectTemplatesEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.ProjectTemplates.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.ProjectTemplates.List, GetProjectTemplates);
        group.MapGet(SupportToolsServerApiRoutes.ProjectTemplates.ByKey, GetProjectTemplateByName);
        group.MapPost(SupportToolsServerApiRoutes.ProjectTemplates.Update, UpdateProjectTemplate);
        group.MapDelete(SupportToolsServerApiRoutes.ProjectTemplates.Delete, DeleteProjectTemplate);

        debugLogger?.Information("{MethodName} Finished", nameof(UseProjectTemplatesEndpoints));

        return true;
    }

    // GET api/v1/projecttemplates
    public static async Task<Results<Ok<List<StsProjectTemplateDataModel>>, ProblemHttpResult>> GetProjectTemplates(
        IQueryHandler<GetProjectTemplatesQuery, List<StsProjectTemplateDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetProjectTemplatesQueryHandler)} from {nameof(GetProjectTemplates)}");

        Result<List<StsProjectTemplateDataModel>> result =
            await handler.Handle(new GetProjectTemplatesQuery(), cancellationToken);

        return result
            .Match<List<StsProjectTemplateDataModel>, Results<Ok<List<StsProjectTemplateDataModel>>,
                ProblemHttpResult>>(projectTemplates => TypedResults.Ok(projectTemplates),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/projecttemplates/{key}
    public static async Task<Results<Ok<StsProjectTemplateDataModel>, ProblemHttpResult>> GetProjectTemplateByName(
        [FromRoute] string key, IQueryHandler<GetProjectTemplateByNameQuery, StsProjectTemplateDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(GetProjectTemplateByNameQueryHandler)} for {name} from {nameof(GetProjectTemplateByName)}");

        Result<StsProjectTemplateDataModel> result =
            await handler.Handle(new GetProjectTemplateByNameQuery(name), cancellationToken);

        return result.Match<StsProjectTemplateDataModel, Results<Ok<StsProjectTemplateDataModel>, ProblemHttpResult>>(
            projectTemplate => TypedResults.Ok(projectTemplate),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/projecttemplates/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateProjectTemplate([FromRoute] string key,
        [FromBody] StsProjectTemplateDataModel projectTemplate,
        ICommandHandler<UpdateProjectTemplateCommand, int> handler, CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        projectTemplate.Name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(UpdateProjectTemplateCommandHandler)} for {projectTemplate.Name} from {nameof(UpdateProjectTemplate)}");

        Result<int> result = await handler.Handle(new UpdateProjectTemplateCommand(projectTemplate), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/projecttemplates/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteProjectTemplate([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteProjectTemplateCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(DeleteProjectTemplateCommandHandler)} for {name} from {nameof(DeleteProjectTemplate)}");

        Result result = await handler.Handle(new DeleteProjectTemplateCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
