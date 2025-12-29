using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace SupportToolsServerApplication;

public static class ServicesExtension
{
    public static IServiceCollection AddAllScopedServiceSupportToolsServerApplication(this IServiceCollection services)
    {
        foreach (var type in typeof(IScopedServiceSupportToolsServerApplication).Assembly.ExportedTypes.Where(x =>
                     typeof(IScopedServiceSupportToolsServerApplication).IsAssignableFrom(x) &&
                     x is { IsInterface: false, IsAbstract: false }))
            services.AddScoped(type);
        return services;
    }

    //public static void AddScopedServices<T>(this IServiceCollection services)
    //{
    //    var assembly = typeof(T).Assembly;
    //    foreach (var type in assembly.ExportedTypes.Where(x =>
    //                 typeof(T).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false }))
    //        services.AddScoped(type);
    //}
}