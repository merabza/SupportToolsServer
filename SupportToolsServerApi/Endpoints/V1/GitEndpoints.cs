using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Routes;
using System.Diagnostics;
using ApiKeyIdentity;
using SystemToolsShared;
using WebInstallers;

namespace SupportToolsServerApi.Endpoints.V1;

internal class GitEndpoints : IInstaller
{
    public int InstallPriority => 50;
    public int ServiceUsePriority => 50;

    public bool InstallServices(WebApplicationBuilder builder, bool debugMode, string[] args,
        Dictionary<string, string> parameters)
    {
        return true;
    }

    public bool UseServices(WebApplication app, bool debugMode)
    {
        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(UseServices)} Started");

        var group = app.MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Git.GitBase)
            .RequireAuthorization();

        group.MapGet(SupportToolsServerApiRoutes.Git.UploadGitRepos, UploadGitRepos);
        //group.MapGet(ProjectsApiRoutes.Projects.GetVersion, GetVersion);
        //group.MapDelete(ProjectsApiRoutes.Projects.RemoveProjectService, RemoveProjectService);
        //group.MapPost(ProjectsApiRoutes.Projects.StartService, StartService);
        //group.MapPost(ProjectsApiRoutes.Projects.StopService, StopService);
        //group.MapPost(ProjectsApiRoutes.Projects.Update, Update);
        //group.MapPost(ProjectsApiRoutes.Projects.UpdateService, UpdateService);
        //group.MapPost(ProjectsApiRoutes.Projects.UpdateSettings, UpdateSettings);

        if (debugMode)
            Console.WriteLine($"{GetType().Name}.{nameof(UseServices)} Finished");

        return true;
    }

    private Task<ActionResult> UploadGitRepos([FromBody]List<GitDataDomain> gits, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }


    
    // POST api/git/uploadgitrepos
    private static async Task<IResult> UploadGitRepos([FromBody] List<GitDataDomain> gits,
        ICurrentUserByApiKey currentUserByApiKey, IMediator mediator, IMessagesDataManager messagesDataManager,
        CancellationToken cancellationToken = default)
    {
        var userName = currentUserByApiKey.Name;
        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} started", cancellationToken);
        Debug.WriteLine($"Call {nameof(UploadGitReposCommandHandler)} from {nameof(UploadGitRepos)}");

        var command = UploadGitReposCommandRequest.Create(userName);
        var result = await mediator.Send(command, cancellationToken);

        await messagesDataManager.SendMessage(userName, $"{nameof(UploadGitRepos)} finished", cancellationToken);
        return result.Match(_ => Results.Ok(), Results.BadRequest);
    }




}