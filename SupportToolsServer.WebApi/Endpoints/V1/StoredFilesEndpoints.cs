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
using SupportToolsServer.Application.StoredFiles.DeleteStoredFile;
using SupportToolsServer.Application.StoredFiles.GetStoredFileByPath;
using SupportToolsServer.Application.StoredFiles.GetStoredFiles;
using SupportToolsServer.Application.StoredFiles.UpdateStoredFile;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;
using WebSystemTools.WebApi.Abstractions.Infrastructure;

namespace SupportToolsServer.WebApi.Endpoints.V1;

//რეესტრის არეალი: საიდუმლო ფაილები (CLAUDE.md, Registry conventions). გზა route-ის key-ში ვერ ჩაჯდება, ამიტომ
//GET-სა და DELETE-ს query-ში (path) მოდის, POST-ს კი ტანში. ASP.NET Core query-ის მნიშვნელობას სრულად ხსნის
//(%5C, %3A, %2F, ...), ამიტომ RouteKeys აქ არ სჭირდება. Debug ტრეისში მხოლოდ გზა იწერება, შიგთავსი არასოდეს
// ReSharper disable once UnusedType.Global
public static class StoredFilesEndpoints
{
    public static bool UseStoredFilesEndpoints(this IEndpointRouteBuilder endpoints, ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(UseStoredFilesEndpoints));

        RouteGroupBuilder group = endpoints
            .MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.StoredFiles.Base)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.StoredFiles.List, GetStoredFiles);
        group.MapGet(SupportToolsServerApiRoutes.StoredFiles.Content, GetStoredFileByPath);
        group.MapPost(SupportToolsServerApiRoutes.StoredFiles.Update, UpdateStoredFile);
        group.MapDelete(SupportToolsServerApiRoutes.StoredFiles.Delete, DeleteStoredFile);

        debugLogger?.Information("{MethodName} Finished", nameof(UseStoredFilesEndpoints));

        return true;
    }

    // GET api/v1/files
    //მხოლოდ მეტამონაცემები, შიგთავსის გარეშე
    public static async Task<Results<Ok<List<StsStoredFileInfoDataModel>>, ProblemHttpResult>> GetStoredFiles(
        IQueryHandler<GetStoredFilesQuery, List<StsStoredFileInfoDataModel>> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(GetStoredFilesQueryHandler)} from {nameof(GetStoredFiles)}");

        Result<List<StsStoredFileInfoDataModel>> result =
            await handler.Handle(new GetStoredFilesQuery(), cancellationToken);

        return result
            .Match<List<StsStoredFileInfoDataModel>,
                Results<Ok<List<StsStoredFileInfoDataModel>>, ProblemHttpResult>>(
                storedFiles => TypedResults.Ok(storedFiles),
                errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // GET api/v1/files/content?path=...
    public static async Task<Results<Ok<StsStoredFileDataModel>, ProblemHttpResult>> GetStoredFileByPath(
        [FromQuery] string path, IQueryHandler<GetStoredFileByPathQuery, StsStoredFileDataModel> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(GetStoredFileByPathQueryHandler)} for {path} from {nameof(GetStoredFileByPath)}");

        Result<StsStoredFileDataModel> result =
            await handler.Handle(new GetStoredFileByPathQuery(path), cancellationToken);

        return result.Match<StsStoredFileDataModel, Results<Ok<StsStoredFileDataModel>, ProblemHttpResult>>(
            storedFile => TypedResults.Ok(storedFile), errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // POST api/v1/files/update
    //გზა ტანშია. ტანის Version მოსალოდნელი ვერსიაა (0 — შექმნა), პასუხი კი ფაილის ახალი ვერსია
    public static async Task<Results<Ok<int>, ProblemHttpResult>> UpdateStoredFile(
        [FromBody] StsStoredFileDataModel storedFile, ICommandHandler<UpdateStoredFileCommand, int> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine(
            $"Call {nameof(UpdateStoredFileCommandHandler)} for {storedFile.Path} from {nameof(UpdateStoredFile)}");

        Result<int> result = await handler.Handle(new UpdateStoredFileCommand(storedFile), cancellationToken);

        return result.Match<int, Results<Ok<int>, ProblemHttpResult>>(version => TypedResults.Ok(version),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }

    // DELETE api/v1/files/delete?path=...&version=N
    //version-ის გარეშე წაშლა უპირობოა
    public static async Task<Results<Ok, ProblemHttpResult>> DeleteStoredFile([FromQuery] string path,
        [FromQuery] int? version, ICommandHandler<DeleteStoredFileCommand> handler,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Call {nameof(DeleteStoredFileCommandHandler)} for {path} from {nameof(DeleteStoredFile)}");

        Result result = await handler.Handle(new DeleteStoredFileCommand(path, version), cancellationToken);

        return result.Match<Results<Ok, ProblemHttpResult>>(() => TypedResults.Ok(),
            errors => (ProblemHttpResult)CustomResults.Problem(errors));
    }
}
