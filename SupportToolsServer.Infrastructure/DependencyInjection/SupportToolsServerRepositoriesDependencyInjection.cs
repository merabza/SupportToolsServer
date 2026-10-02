using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;

namespace SupportToolsServer.Infrastructure.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerRepositoriesDependencyInjection
{
    public static IServiceCollection AddSupportToolsServerRepositories(this IServiceCollection services,
        ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(AddSupportToolsServerRepositories));

        services.AddScoped<IGitIgnoreFileTypeRepository, GitIgnoreFileTypeRepository>();
        services.AddScoped<IGitRepoRepository, GitRepoRepository>();
        services.AddScoped<IEditorConfigFileTypeRepository, EditorConfigFileTypeRepository>();
        services.AddScoped<IDeploymentEnvironmentRepository, DeploymentEnvironmentRepository>();

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerRepositories));

        return services;
    }
}
