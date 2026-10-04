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
using SupportToolsServer.Application.FileStorages.DeleteFileStorage;
using SupportToolsServer.Application.FileStorages.GetFileStorageByName;
using SupportToolsServer.Application.FileStorages.GetFileStorages;
using SupportToolsServer.Application.FileStorages.UpdateFileStorage;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: ფაილსაცავები (CLAUDE.md, Registry conventions). ჩანაწერში პაროლია, ამიტომ ტრეისში მხოლოდ სახელი იწერება
// ReSharper disable once UnusedType.Global
public static class FileStoragesEndpoints
{
    public static bool UseFileStoragesEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseFileStoragesEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.FileStorages.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.FileStorages.List, GetFileStorages);
        group.MapGet(SupportToolsServerApiRoutes.FileStorages.ByKey, GetFileStorageByName);
        group.MapPost(SupportToolsServerApiRoutes.FileStorages.Update, UpdateFileStorage);
        group.MapDelete(SupportToolsServerApiRoutes.FileStorages.Delete, DeleteFileStorage);

        debugLogger?.Information("{MethodName} Finished", nameof(UseFileStoragesEndpoints));

        return true;
    }

    // GET api/v1/filestorages
    public static async Task<Results<Ok<List<StsFileStorageDataModel>>, ProblemHttpResult>> GetFileStorages(
        IQueryHandler<GetFileStoragesQuery, List<StsFileStorageDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetFileStoragesQueryHandler)} from {nameof(GetFileStorages)}");

        Result<List<StsFileStorageDataModel>> result =
            await handler.Handle(new GetFileStoragesQuery(), cancellationToken);

        return result
            .Match<List<StsFileStorageDataModel>, Results<Ok<List<StsFileStorageDataModel>>, ProblemHttpResult>>(
                fileStorages => TypedResults.Ok(fileStorages),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/filestorages/{key}
    public static async Task<Results<Ok<StsFileStorageDataModel>, ProblemHttpResult>> GetFileStorageByName(
        [FromRoute] string key, IQueryHandler<GetFileStorageByNameQuery, StsFileStorageDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(GetFileStorageByNameQueryHandler)} for {name} from {nameof(GetFileStorageByName)}");

        Result<StsFileStorageDataModel> result =
            await handler.Handle(new GetFileStorageByNameQuery(name), cancellationToken);

        return result.Match<StsFileStorageDataModel, Results<Ok<StsFileStorageDataModel>, ProblemHttpResult>>(
            fileStorage => TypedResults.Ok(fileStorage), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/filestorages/update/{key}
    //ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ჩანაწერის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateFileStorage([FromRoute] string key,
        [FromBody] StsFileStorageDataModel fileStorage, ICommandHandler<UpdateFileStorageCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        //მისამართში მითითებული სახელი უპირატესია ტანში გადმოცემულზე
        fileStorage.Name = RouteKeys.Decode(key);
        Debug.WriteLine(
            $"Call {nameof(UpdateFileStorageCommandHandler)} for {fileStorage.Name} from {nameof(UpdateFileStorage)}");

        Result<int> result = await handler.Handle(new UpdateFileStorageCommand(fileStorage), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/filestorages/delete/{key}?version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteFileStorage([FromRoute] string key,
        [FromQuery] int? version, ICommandHandler<DeleteFileStorageCommand> handler,
        CancellationToken cancellationToken = default)
    {
        string name = RouteKeys.Decode(key);
        Debug.WriteLine($"Call {nameof(DeleteFileStorageCommandHandler)} for {name} from {nameof(DeleteFileStorage)}");

        Result result = await handler.Handle(new DeleteFileStorageCommand(name, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
