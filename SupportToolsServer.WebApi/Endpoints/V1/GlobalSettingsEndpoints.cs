using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Serilog;
using SupportToolsServer.Application.Settings.GetGlobalSettings;
using SupportToolsServer.Application.Settings.UpdateGlobalSettings;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის singleton: გლობალური პარამეტრები (CLAUDE.md, Registry conventions). ჩანაწერი ერთადერთია, ამიტომ route-ს
//key არ აქვს და წაშლის endpoint არ არის. ტანში MediatRLicenseKey-ია, ამიტომ ტრეისი მხოლოდ ჩანაწერს ასახელებს
// ReSharper disable once UnusedType.Global
public static class GlobalSettingsEndpoints
{
    public static bool UseGlobalSettingsEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseGlobalSettingsEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.GlobalSettings.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.GlobalSettings.Get, GetGlobalSettings);
        group.MapPost(SupportToolsServerApiRoutes.GlobalSettings.Update, UpdateGlobalSettings);

        debugLogger?.Information("{MethodName} Finished", nameof(UseGlobalSettingsEndpoints));

        return true;
    }

    // GET api/v1/settings/global
    //სანამ ჩანაწერი შეიქმნება, პასუხი ცარიელი კონტრაქტია Version = 0-ით
    public static async Task<Results<Ok<StsGlobalSettingsDataModel>, ProblemHttpResult>> GetGlobalSettings(
        IQueryHandler<GetGlobalSettingsQuery, StsGlobalSettingsDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetGlobalSettingsQueryHandler)} from {nameof(GetGlobalSettings)}");

        Result<StsGlobalSettingsDataModel> result =
            await handler.Handle(new GetGlobalSettingsQuery(), cancellationToken);

        return result.Match<StsGlobalSettingsDataModel, Results<Ok<StsGlobalSettingsDataModel>, ProblemHttpResult>>(
            globalSettings => TypedResults.Ok(globalSettings),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/settings/global/update
    //ტანის Version მოსალოდნელი ვერსიაა (0 — პირველი შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateGlobalSettings(
        [FromBody] StsGlobalSettingsDataModel globalSettings,
        ICommandHandler<UpdateGlobalSettingsCommand, int> handler, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(UpdateGlobalSettingsCommandHandler)} from {nameof(UpdateGlobalSettings)}");

        Result<int> result = await handler.Handle(new UpdateGlobalSettingsCommand(globalSettings), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
