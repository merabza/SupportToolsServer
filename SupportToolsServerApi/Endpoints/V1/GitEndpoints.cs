using Microsoft.AspNetCore.Builder;
using SupportToolsServerContracts.V1.Routes;
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

        var group = app.MapGroup(SupportToolsServerApiRoutes.ApiBase + SupportToolsServerApiRoutes.Projects.ProjectBase)
            .RequireAuthorization();

        //group.MapGet(ProjectsApiRoutes.Projects.GetAppSettingsVersion, GetAppSettingsVersion);
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
}