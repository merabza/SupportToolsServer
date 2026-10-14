using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using SupportToolsServer.Application.GitRepos.GitRemoteHeads;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.DependencyInjection;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Infrastructure.Options;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.DependencyInjection;

public sealed class SupportToolsServerGitProjectsDependencyInjectionTests
{
    private static IConfiguration CreateConfiguration(string? workFolder, string? gitProjectsRefreshHours = null,
        string? remoteHeadsCacheMinutes = null)
    {
        var settings = new Dictionary<string, string?> { ["AppOptions:WorkFolder"] = workFolder };
        if (gitProjectsRefreshHours is not null)
        {
            settings["AppOptions:GitProjectsRefreshHours"] = gitProjectsRefreshHours;
        }

        if (remoteHeadsCacheMinutes is not null)
        {
            settings["AppOptions:GitRemoteHeadsCacheMinutes"] = remoteHeadsCacheMinutes;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static ServiceProvider BuildProvider(string? workFolder, string? gitProjectsRefreshHours = null,
        string? remoteHeadsCacheMinutes = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSupportToolsServerGitProjects(null,
            CreateConfiguration(workFolder, gitProjectsRefreshHours, remoteHeadsCacheMinutes));
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
                 d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IGitProjectFilesScanner) &&
                 d.ImplementationType == typeof(GitProjectFilesScanner) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services,
            d => d.ServiceType == typeof(IHostedService) &&
                 d.ImplementationType == typeof(GitProjectsRefreshBackgroundService));
        Assert.Contains(services,
            d => d.ServiceType == typeof(IGitRemoteHeadsCache) && d.ImplementationType == typeof(GitRemoteHeadsCache) &&
                 d.Lifetime == ServiceLifetime.Singleton);
    }

    //One cache for every request, built with the services that the module registers
    [Fact]
    public void AddSupportToolsServerGitProjects_ResolvesTheSingleRemoteHeadsCache()
    {
        using ServiceProvider provider = BuildProvider("Work");
        using IServiceScope scope = provider.CreateScope();

        var cache = provider.GetRequiredService<IGitRemoteHeadsCache>();

        Assert.IsType<GitRemoteHeadsCache>(cache);
        Assert.Same(cache, scope.ServiceProvider.GetRequiredService<IGitRemoteHeadsCache>());
    }

    [Fact]
    public void AddSupportToolsServerGitProjects_BindsTheRemoteHeadsCacheLifetime()
    {
        using ServiceProvider defaultProvider = BuildProvider("Work");
        using ServiceProvider provider = BuildProvider("Work", remoteHeadsCacheMinutes: "3");

        Assert.Equal(10,
            defaultProvider.GetRequiredService<IOptions<AppOptions>>().Value.GitRemoteHeadsCacheMinutes);
        Assert.Equal(3, provider.GetRequiredService<IOptions<AppOptions>>().Value.GitRemoteHeadsCacheMinutes);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1441")]
    public void AddSupportToolsServerGitProjects_FailsTheStartupValidation_WhenTheCacheLifetimeIsOutOfRange(
        string remoteHeadsCacheMinutes)
    {
        using ServiceProvider provider = BuildProvider("Work", remoteHeadsCacheMinutes: remoteHeadsCacheMinutes);

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Equal("AppOptions:GitRemoteHeadsCacheMinutes must be between 0 and 1440",
            Assert.Single(exception.Failures));
    }

    //0 checks the remote on every request
    [Theory]
    [InlineData("0")]
    [InlineData("1440")]
    public void AddSupportToolsServerGitProjects_PassesTheStartupValidation_WhenTheCacheLifetimeIsInRange(
        string remoteHeadsCacheMinutes)
    {
        using ServiceProvider provider = BuildProvider("Work", remoteHeadsCacheMinutes: remoteHeadsCacheMinutes);

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    //The clock of the periodic refresh is the system clock, unless the host registered one before (the authentication
    //registers it as well)
    [Fact]
    public void AddSupportToolsServerGitProjects_RegistersTheSystemClockOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);

        services.AddSupportToolsServerGitProjects(null, CreateConfiguration("Work"));

        Assert.Single(services, d => d.ServiceType == typeof(TimeProvider));
        using ServiceProvider provider = BuildProvider("Work");
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    //The scanner and the refresh service resolve with the services that the module registers
    [Fact]
    public void AddSupportToolsServerGitProjects_ResolvesTheScannerAndTheRefreshService()
    {
        using ServiceProvider provider = BuildProvider("Work");
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<GitProjectFilesScanner>(scope.ServiceProvider.GetRequiredService<IGitProjectFilesScanner>());
        Assert.Contains(provider.GetServices<IHostedService>(), x => x is GitProjectsRefreshBackgroundService);
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
        Assert.Equal(24, options.GitProjectsRefreshHours);
    }

    [Fact]
    public void AddSupportToolsServerGitProjects_BindsTheRefreshPeriod()
    {
        using ServiceProvider provider = BuildProvider("Work", "6");

        Assert.Equal(6, provider.GetRequiredService<IOptions<AppOptions>>().Value.GitProjectsRefreshHours);
    }

    //A period longer than a month would not fit the timer, a negative one means nothing
    [Theory]
    [InlineData("-1")]
    [InlineData("721")]
    public void AddSupportToolsServerGitProjects_FailsTheStartupValidation_WhenTheRefreshPeriodIsOutOfRange(
        string gitProjectsRefreshHours)
    {
        using ServiceProvider provider = BuildProvider("Work", gitProjectsRefreshHours);

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Equal("AppOptions:GitProjectsRefreshHours must be between 0 and 720", Assert.Single(exception.Failures));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("720")]
    public void AddSupportToolsServerGitProjects_PassesTheStartupValidation_WhenTheRefreshPeriodIsInRange(
        string gitProjectsRefreshHours)
    {
        using ServiceProvider provider = BuildProvider("Work", gitProjectsRefreshHours);

        provider.GetRequiredService<IStartupValidator>().Validate();
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
