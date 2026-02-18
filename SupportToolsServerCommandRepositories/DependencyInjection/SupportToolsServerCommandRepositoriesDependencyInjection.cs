using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SupportToolsServerApplication.Repositories.GitIgnoreFileTypes;
using SupportToolsServerApplication.Repositories.Gits;

namespace SupportToolsServerCommandRepositories.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerCommandRepositoriesDependencyInjection
{
    public static IServiceCollection AddSupportToolsServerCommandRepositories(this IServiceCollection services,
        ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(AddSupportToolsServerCommandRepositories));

        services.AddScoped<IGitsCommandsRepository, GitsCommandsRepository>();
        services.AddScoped<IGitIgnoreFileTypesCommandsRepository, GitIgnoreFileTypesCommandsRepository>();

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServerCommandRepositories));

        return services;
    }
}
