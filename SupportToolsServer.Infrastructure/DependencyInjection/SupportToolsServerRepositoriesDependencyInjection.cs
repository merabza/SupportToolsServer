using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.DotnetTools;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.SmartSchemas;

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
        services.AddScoped<IRuntimeRepository, RuntimeRepository>();
        services.AddScoped<INpmPackageRepository, NpmPackageRepository>();
        services.AddScoped<IReactAppTemplateRepository, ReactAppTemplateRepository>();
        services.AddScoped<IDotnetToolRepository, DotnetToolRepository>();
        services.AddScoped<ISmartSchemaRepository, SmartSchemaRepository>();
        services.AddScoped<IFileStorageRepository, FileStorageRepository>();
        services.AddScoped<IApiClientRepository, ApiClientRepository>();
        services.AddScoped<IDatabaseServerConnectionRepository, DatabaseServerConnectionRepository>();

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerRepositories));

        return services;
    }
}
