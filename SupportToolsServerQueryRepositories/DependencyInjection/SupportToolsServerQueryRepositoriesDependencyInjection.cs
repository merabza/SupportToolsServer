using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SupportToolsServerApiKeyIdentity;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Repositories.Gits;

namespace SupportToolsServerQueryRepositories.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerQueryRepositoriesDependencyInjection
{
    public static IServiceCollection AddSupportToolsServerQueryRepositories(this IServiceCollection services,
        ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(AddSupportToolsServerQueryRepositories));

        services.AddScoped<IGitsQueriesRepository, GitsQueriesRepository>();
        services.AddScoped<IGitIgnoreFileTypesQueriesRepository, GitIgnoreFileTypesQueriesRepository>();
        services.AddScoped<IApiKeyQueriesRepository, ApiKeyQueriesRepository>();

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerQueryRepositories));

        return services;
    }
}
