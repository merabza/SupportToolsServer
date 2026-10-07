using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using SupportToolsServer.Infrastructure.DependencyInjection;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.DotnetTools;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepoProjects;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.ProjectTemplates;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SupportToolsServerCore.Domain.Runtimes;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;
using SupportToolsServerCore.Domain.StoredFiles;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.DependencyInjection;

public sealed class SupportToolsServerRepositoriesDependencyInjectionTests
{
    [Fact]
    public void AddSupportToolsServerRepositories_RegistersEveryRepositoryAsScoped()
    {
        var services = new ServiceCollection();

        IServiceCollection returned = services.AddSupportToolsServerRepositories(null);

        Assert.Same(services, returned);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IGitIgnoreFileTypeRepository) &&
                 d.ImplementationType == typeof(GitIgnoreFileTypeRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IGitRepoRepository) && d.ImplementationType == typeof(GitRepoRepository) &&
                 d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IEditorConfigFileTypeRepository) &&
                 d.ImplementationType == typeof(EditorConfigFileTypeRepository) &&
                 d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IDeploymentEnvironmentRepository) &&
                 d.ImplementationType == typeof(DeploymentEnvironmentRepository) &&
                 d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IRuntimeRepository) && d.ImplementationType == typeof(RuntimeRepository) &&
                 d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(INpmPackageRepository) &&
                 d.ImplementationType == typeof(NpmPackageRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IReactAppTemplateRepository) &&
                 d.ImplementationType == typeof(ReactAppTemplateRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IDotnetToolRepository) &&
                 d.ImplementationType == typeof(DotnetToolRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(ISmartSchemaRepository) &&
                 d.ImplementationType == typeof(SmartSchemaRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IFileStorageRepository) &&
                 d.ImplementationType == typeof(FileStorageRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IApiClientRepository) &&
                 d.ImplementationType == typeof(ApiClientRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IDatabaseServerConnectionRepository) &&
                 d.ImplementationType == typeof(DatabaseServerConnectionRepository) &&
                 d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IServerRepository) && d.ImplementationType == typeof(ServerRepository) &&
                 d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IGlobalSettingsRepository) &&
                 d.ImplementationType == typeof(GlobalSettingsRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IProjectCreatorSettingsRepository) &&
                 d.ImplementationType == typeof(ProjectCreatorSettingsRepository) &&
                 d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IProjectTemplateRepository) &&
                 d.ImplementationType == typeof(ProjectTemplateRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IProjectRepository) && d.ImplementationType == typeof(ProjectRepository) &&
                 d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IStoredFileRepository) &&
                 d.ImplementationType == typeof(StoredFileRepository) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IGitRepoProjectRepository) &&
                 d.ImplementationType == typeof(GitRepoProjectRepository) && d.Lifetime == ServiceLifetime.Scoped);
    }

    //UpdateStoredFile takes the time of the change from it
    [Fact]
    public void AddSupportToolsServerRepositories_RegistersTheSystemClock()
    {
        var services = new ServiceCollection();

        services.AddSupportToolsServerRepositories(null);

        ServiceDescriptor clock = Assert.Single(services, d => d.ServiceType == typeof(TimeProvider));
        Assert.Same(TimeProvider.System, clock.ImplementationInstance);
        Assert.Equal(ServiceLifetime.Singleton, clock.Lifetime);
    }

    //The authentication of the host registers the clock as well, and the one registered first is kept
    [Fact]
    public void AddSupportToolsServerRepositories_KeepsAClockThatIsAlreadyRegistered()
    {
        var services = new ServiceCollection();
        var clock = new Mock<TimeProvider>();
        services.AddSingleton(clock.Object);

        services.AddSupportToolsServerRepositories(null);

        Assert.Same(clock.Object, Assert.Single(services, d => d.ServiceType == typeof(TimeProvider))
            .ImplementationInstance);
    }

    [Fact]
    public void AddSupportToolsServerRepositories_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        new ServiceCollection().AddSupportToolsServerRepositories(logger.Object);

        logger.Verify(l => l.Information("{MethodName} Started", "AddSupportToolsServerRepositories"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "AddSupportToolsServerRepositories"), Times.Once);
    }
}
