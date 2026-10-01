using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Infrastructure.Options;

namespace SupportToolsServer.Infrastructure.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerGitProjectsDependencyInjection
{
    //გიტის ჩანაწერის დამატების ან რედაქტირებისას სამუშაო ფოლდერში მისი პროექტის განახლება
    public static IServiceCollection AddSupportToolsServerGitProjects(this IServiceCollection services,
        ILogger? debugLogger, IConfiguration configuration)
    {
        debugLogger?.Information("{MethodName} Started", nameof(AddSupportToolsServerGitProjects));

        services.AddOptions<AppOptions>().Bind(configuration.GetSection(AppOptions.SectionName)).Validate(
            options => !string.IsNullOrWhiteSpace(options.WorkFolder),
            $"{AppOptions.SectionName}:{nameof(AppOptions.WorkFolder)} is not specified").ValidateOnStart();

        services.AddSingleton<GitProjectUpdateQueue>();
        services.AddSingleton<IGitProjectUpdateQueue>(sp => sp.GetRequiredService<GitProjectUpdateQueue>());
        services.AddHostedService<GitProjectUpdateBackgroundService>();

        services.AddScoped<IGitsWorkFolder, GitsWorkFolder>();
        services.AddScoped<IGitClient, GitClient>();

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerGitProjects));

        return services;
    }
}
