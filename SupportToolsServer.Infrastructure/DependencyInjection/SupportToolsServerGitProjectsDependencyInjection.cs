using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
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
            .ValidateOnStart();

        services.AddSingleton<GitProjectUpdateQueue>();
        services.AddSingleton<IGitProjectUpdateQueue>(sp => sp.GetRequiredService<GitProjectUpdateQueue>());
        services.AddHostedService<GitProjectUpdateBackgroundService>();

        //პერიოდული განახლების საათი. ჰოსტში მას ავთენტიფიკაციაც ამატებს, ამიტომ TryAdd
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<GitProjectsRefreshBackgroundService>();

        services.AddScoped<IGitsWorkFolder, GitsWorkFolder>();
        services.AddScoped<IGitClient, GitClient>();
        services.AddScoped<IGitProjectFilesScanner, GitProjectFilesScanner>();

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerGitProjects));

        return services;
    }
}
