using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Serilog;
using SupportToolsServer.Application.Settings.GetProjectCreatorSettings;
using SupportToolsServer.Application.Settings.UpdateProjectCreatorSettings;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის singleton: პროექტის შემქმნელის პარამეტრები (CLAUDE.md, Registry conventions). ჩანაწერი ერთადერთია, ამიტომ
//route-ს key არ აქვს და წაშლის endpoint არ არის
// ReSharper disable once UnusedType.Global
public static class ProjectCreatorSettingsEndpoints
{
    public static bool UseProjectCreatorSettingsEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseProjectCreatorSettingsEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.ProjectCreatorSettings.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.ProjectCreatorSettings.Get, GetProjectCreatorSettings);
        group.MapPost(SupportToolsServerApiRoutes.ProjectCreatorSettings.Update, UpdateProjectCreatorSettings);

        debugLogger?.Information("{MethodName} Finished", nameof(UseProjectCreatorSettingsEndpoints));

        return true;
    }

    // GET api/v1/settings/projectcreator
    //სანამ ჩანაწერი შეიქმნება, პასუხი ცარიელი კონტრაქტია Version = 0-ით
    public static async Task<Results<Ok<StsProjectCreatorSettingsDataModel>, ProblemHttpResult>>
        GetProjectCreatorSettings(
            IQueryHandler<GetProjectCreatorSettingsQuery, StsProjectCreatorSettingsDataModel> handler,
            CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(GetProjectCreatorSettingsQueryHandler)} from {nameof(GetProjectCreatorSettings)}");

        Result<StsProjectCreatorSettingsDataModel> result =
            await handler.Handle(new GetProjectCreatorSettingsQuery(), cancellationToken);

        return result
            .Match<StsProjectCreatorSettingsDataModel,
                Results<Ok<StsProjectCreatorSettingsDataModel>, ProblemHttpResult>>(
                projectCreatorSettings => TypedResults.Ok(projectCreatorSettings),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/settings/projectcreator/update
    //ტანის Version მოსალოდნელი ვერსიაა (0 — პირველი შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateProjectCreatorSettings(
        [FromBody] StsProjectCreatorSettingsDataModel projectCreatorSettings,
        ICommandHandler<UpdateProjectCreatorSettingsCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(UpdateProjectCreatorSettingsCommandHandler)} from {nameof(UpdateProjectCreatorSettings)}");

        Result<int> result = await handler.Handle(new UpdateProjectCreatorSettingsCommand(projectCreatorSettings),
            cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
