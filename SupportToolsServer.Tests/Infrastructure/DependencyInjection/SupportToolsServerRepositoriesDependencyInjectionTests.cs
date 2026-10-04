using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using SupportToolsServer.Infrastructure.DependencyInjection;
using SupportToolsServer.Infrastructure.Repositories;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.DotnetTools;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SupportToolsServerCore.Domain.Runtimes;
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
