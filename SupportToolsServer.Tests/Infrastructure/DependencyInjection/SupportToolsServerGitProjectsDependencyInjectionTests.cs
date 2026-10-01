using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.DependencyInjection;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Infrastructure.Options;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.DependencyInjection;

public sealed class SupportToolsServerGitProjectsDependencyInjectionTests
{
    private static IConfiguration CreateConfiguration(string? workFolder)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AppOptions:WorkFolder"] = workFolder }).Build();
    }

    private static ServiceProvider BuildProvider(string? workFolder)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSupportToolsServerGitProjects(null, CreateConfiguration(workFolder));
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddSupportToolsServerGitProjects_RegistersTheGitServices()
    {
        var services = new ServiceCollection();

        IServiceCollection returned = services.AddSupportToolsServerGitProjects(null, CreateConfiguration("Work"));

        Assert.Same(services, returned);
        Assert.Contains(services,
            d => d.ServiceType == typeof(GitProjectUpdateQueue) && d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IHostedService) &&
                 d.ImplementationType == typeof(GitProjectUpdateBackgroundService));
        Assert.Contains(services,
            d => d.ServiceType == typeof(IGitsWorkFolder) && d.ImplementationType == typeof(GitsWorkFolder) &&
                 d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IGitClient) && d.ImplementationType == typeof(GitClient) &&
                 d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSupportToolsServerGitProjects_ResolvesTheQueueAbstractionToTheSingleQueue()
    {
        using ServiceProvider provider = BuildProvider("Work");

        var queue = provider.GetRequiredService<IGitProjectUpdateQueue>();

        Assert.Same(provider.GetRequiredService<GitProjectUpdateQueue>(), queue);
        Assert.Same(queue, provider.GetRequiredService<IGitProjectUpdateQueue>());
    }

    [Fact]
    public void AddSupportToolsServerGitProjects_BindsTheAppOptionsSection()
    {
        using ServiceProvider provider = BuildProvider(@"C:\Work");

        AppOptions options = provider.GetRequiredService<IOptions<AppOptions>>().Value;

        Assert.Equal(@"C:\Work", options.WorkFolder);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddSupportToolsServerGitProjects_FailsTheStartupValidation_WhenTheWorkFolderIsEmpty(string? workFolder)
    {
        using ServiceProvider provider = BuildProvider(workFolder);

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Equal("AppOptions:WorkFolder is not specified", Assert.Single(exception.Failures));
    }

    [Fact]
    public void AddSupportToolsServerGitProjects_PassesTheStartupValidation_WhenTheWorkFolderIsSpecified()
    {
        using ServiceProvider provider = BuildProvider("Work");

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    [Fact]
    public void AddSupportToolsServerGitProjects_LogsTheStartAndTheEnd()
    {
        var logger = new Mock<ILogger>();

        new ServiceCollection().AddSupportToolsServerGitProjects(logger.Object, CreateConfiguration("Work"));

        logger.Verify(l => l.Information("{MethodName} Started", "AddSupportToolsServerGitProjects"), Times.Once);
        logger.Verify(l => l.Information("{MethodName} Finished", "AddSupportToolsServerGitProjects"), Times.Once);
    }
}
