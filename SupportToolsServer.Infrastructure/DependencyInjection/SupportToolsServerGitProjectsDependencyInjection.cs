using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using SupportToolsServer.Application.GitRepos.GitRemoteHeads;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Infrastructure.Options;

namespace SupportToolsServer.Infrastructure.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerGitProjectsDependencyInjection
{
    //გიტის ჩანაწერის დამატების ან რედაქტირებისას, აგრეთვე სტარტზე და პერიოდულად, სამუშაო ფოლდერში მისი პროექტის
    //განახლება და პროექტების სკანირება
    public static IServiceCollection AddSupportToolsServerGitProjects(this IServiceCollection services,
        ILogger? debugLogger, IConfiguration configuration)
    {
        debugLogger?.Information("{MethodName} Started", nameof(AddSupportToolsServerGitProjects));

        services.AddOptions<AppOptions>().Bind(configuration.GetSection(AppOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.WorkFolder),
                $"{AppOptions.SectionName}:{nameof(AppOptions.WorkFolder)} is not specified")
            .Validate(options => options.GitProjectsRefreshHours is >= 0 and <= AppOptions.MaxGitProjectsRefreshHours,
                $"{AppOptions.SectionName}:{nameof(AppOptions.GitProjectsRefreshHours)} must be between 0 and " +
                $"{AppOptions.MaxGitProjectsRefreshHours}")
            .Validate(
                options => options.GitRemoteHeadsCacheMinutes is >= 0 and <= AppOptions.MaxGitRemoteHeadsCacheMinutes,
                $"{AppOptions.SectionName}:{nameof(AppOptions.GitRemoteHeadsCacheMinutes)} must be between 0 and " +
                $"{AppOptions.MaxGitRemoteHeadsCacheMinutes}")
            .ValidateOnStart();

        services.AddSingleton<GitProjectUpdateQueue>();
        services.AddSingleton<IGitProjectUpdateQueue>(sp => sp.GetRequiredService<GitProjectUpdateQueue>());
        services.AddHostedService<GitProjectUpdateBackgroundService>();

        //პერიოდული განახლების საათი. ჰოსტში მას ავთენტიფიკაციაც ამატებს, ამიტომ TryAdd
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<GitProjectsRefreshBackgroundService>();

        services.AddScoped<IGitsWorkFolder, GitsWorkFolder>();
        //GitClient მდგომარეობას არ ინახავს, ამიტომ singleton-ია: მას singleton ქეშიც იყენებს
        services.AddSingleton<IGitClient, GitClient>();
        services.AddScoped<IGitProjectFilesScanner, GitProjectFilesScanner>();
        services.AddSingleton<IGitRemoteHeadsCache, GitRemoteHeadsCache>();

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerGitProjects));

        return services;
    }
}
