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
using SupportToolsServer.Application.NpmPackages.DeleteNpmPackage;
using SupportToolsServer.Application.NpmPackages.GetNpmPackageByName;
using SupportToolsServer.Application.NpmPackages.GetNpmPackages;
using SupportToolsServer.Application.NpmPackages.UpdateNpmPackage;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: npm-ის პაკეტები (CLAUDE.md, Registry conventions)
// ReSharper disable once UnusedType.Global
public static class NpmPackagesEndpoints
{
    public static bool UseNpmPackagesEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseNpmPackagesEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.NpmPackages.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.NpmPackages.List, GetNpmPackages);
        group.MapGet(SupportToolsServerApiRoutes.NpmPackages.ByKey, GetNpmPackageByName);
        group.MapPost(SupportToolsServerApiRoutes.NpmPackages.Update, UpdateNpmPackage);
        group.MapDelete(SupportToolsServerApiRoutes.NpmPackages.Delete, DeleteNpmPackage);

        debugLogger?.Information("{MethodName} Finished", nameof(UseNpmPackagesEndpoints));

        return true;
    }

    // GET api/v1/npmpackages
    public static async Task<Results<Ok<List<StsNpmPackageDataModel>>, ProblemHttpResult>> GetNpmPackages(
        IQueryHandler<GetNpmPackagesQuery, List<StsNpmPackageDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetNpmPackagesQueryHandler)} from {nameof(GetNpmPackages)}");

        Result<List<StsNpmPackageDataModel>> result =
            await handler.Handle(new GetNpmPackagesQuery(), cancellationToken);

        return result.Match<List<StsNpmPackageDataModel>, Results<Ok<List<StsNpmPackageDataModel>>, ProblemHttpResult>>(
            npmPackages => TypedResults.Ok(npmPackages), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/npmpackages/{key}
    public static async Task<Results<Ok<StsNpmPackageDataModel>, ProblemHttpResult>> GetNpmPackageByName(
        [FromRoute] string key, IQueryHandler<GetNpmPackageByNameQuery, StsNpmPackageDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(GetNpmPackageByNameQueryHandler)} for {name} from {nameof(GetNpmPackageByName)}");

        Result<StsNpmPackageDataModel> result =
            await handler.Handle(new GetNpmPackageByNameQuery(name), cancellationToken);

        return result.Match<StsNpmPackageDataModel, Results<Ok<StsNpmPackageDataModel>, ProblemHttpResult>>(
            npmPackage => TypedResults.Ok(npmPackage), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/npmpackages/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateNpmPackage([FromRoute] string key,
        [FromBody] StsNpmPackageDataModel npmPackage, ICommandHandler<UpdateNpmPackageCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        npmPackage.Name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(UpdateNpmPackageCommandHandler)} for {npmPackage.Name} from {nameof(UpdateNpmPackage)}");

        Result<int> result = await handler.Handle(new UpdateNpmPackageCommand(npmPackage), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/npmpackages/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteNpmPackage([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteNpmPackageCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(DeleteNpmPackageCommandHandler)} for {name} from {nameof(DeleteNpmPackage)}");

        Result result = await handler.Handle(new DeleteNpmPackageCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
