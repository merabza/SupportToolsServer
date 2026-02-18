using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SupportToolsServer.Domain.GitIgnoreFileTypes;
using SystemTools.DomainShared.Repositories;

namespace SupportToolsServer.Repositories.DependencyInjection;

// ReSharper disable once UnusedType.Global
public static class SupportToolsServerRepositoriesDependencyInjection
{
    public static IServiceCollection AddSupportToolsServer_Repositories(this IServiceCollection services,
        ILogger? debugLogger)
    {
        debugLogger?.Information("{MethodName} Started", nameof(AddSupportToolsServer_Repositories));

        //builder.Services.AddScoped<IGitsQueriesRepository, GitsQueriesRepository>();
        //builder.Services.AddScoped<IGitIgnoreFileTypesQueriesRepository, GitIgnoreFileTypesQueriesRepository>();
        services.AddScoped<IUnitOfWork, SupportToolsServerUnitOfWork>();
        services.AddScoped<IGitIgnoreFileTypeRepository, GitIgnoreFileTypeRepository>();

        debugLogger?.Information("{MethodName} Finished", nameof(AddSupportToolsServer_Repositories));

        return services;
    }
}
