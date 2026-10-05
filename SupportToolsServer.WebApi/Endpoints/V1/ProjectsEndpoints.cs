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
using SupportToolsServer.Application.Projects.DeleteProject;
using SupportToolsServer.Application.Projects.GetProjectByName;
using SupportToolsServer.Application.Projects.GetProjects;
using SupportToolsServer.Application.Projects.UpdateProject;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: პროექტები, ბაზის პარამეტრებითა და შვილებით, ერთ აგრეგატად (CLAUDE.md, Registry conventions).
//ჩანაწერში KeyGuidPart-ია, ამიტომ ტრეისში მხოლოდ სახელი იწერება
// ReSharper disable once UnusedType.Global
public static class ProjectsEndpoints
{
    public static bool UseProjectsEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseProjectsEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Projects.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.Projects.List, GetProjects);
        group.MapGet(SupportToolsServerApiRoutes.Projects.ByKey, GetProjectByName);
        group.MapPost(SupportToolsServerApiRoutes.Projects.Update, UpdateProject);
        group.MapDelete(SupportToolsServerApiRoutes.Projects.Delete, DeleteProject);

        debugLogger?.Information("{MethodName} Finished", nameof(UseProjectsEndpoints));

        return true;
    }

    // GET api/v1/projects
    public static async Task<Results<Ok<List<StsProjectDataModel>>, ProblemHttpResult>> GetProjects(
        IQueryHandler<GetProjectsQuery, List<StsProjectDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetProjectsQueryHandler)} from {nameof(GetProjects)}");

        Result<List<StsProjectDataModel>> result = await handler.Handle(new GetProjectsQuery(), cancellationToken);

        return result.Match<List<StsProjectDataModel>, Results<Ok<List<StsProjectDataModel>>, ProblemHttpResult>>(
            projects => TypedResults.Ok(projects), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/projects/{key}
    public static async Task<Results<Ok<StsProjectDataModel>, ProblemHttpResult>> GetProjectByName(
        [FromRoute] string key, IQueryHandler<GetProjectByNameQuery, StsProjectDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(GetProjectByNameQueryHandler)} for {name} from {nameof(GetProjectByName)}");

        Result<StsProjectDataModel> result =
            await handler.Handle(new GetProjectByNameQuery(name), cancellationToken);

        return result.Match<StsProjectDataModel, Results<Ok<StsProjectDataModel>, ProblemHttpResult>>(
            project => TypedResults.Ok(project), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/projects/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateProject([FromRoute] string key,
        [FromBody] StsProjectDataModel project, ICommandHandler<UpdateProjectCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        project.Name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(UpdateProjectCommandHandler)} for {project.Name} from {nameof(UpdateProject)}");

        Result<int> result = await handler.Handle(new UpdateProjectCommand(project), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/projects/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteProject([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteProjectCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(DeleteProjectCommandHandler)} for {name} from {nameof(DeleteProject)}");

        Result result = await handler.Handle(new DeleteProjectCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
