//using System;
//using ApiKeyIdentity;
//using ApiKeysManagement;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.DependencyInjection.Extensions;

//namespace SupportToolsServerApiKeyIdentity;

//public static class SupportToolsServerApiKeyIdentityDependencyInjection
//{
//    public static IServiceCollection AddSupportToolsServerApiKeyIdentity(this IServiceCollection services,
//        bool debugMode)
//    {
//        if (debugMode)
//            Console.WriteLine($"{nameof(AddSupportToolsServerApiKeyIdentity)} Started");

//        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
//        services.AddScoped<ICurrentUserByApiKey, CurrentUserByApiKey>();

//        services.AddScoped<IApiKeyFinder, ApiKeyByDatabaseFinder>();

//        services.AddAuthentication(x => x.DefaultAuthenticateScheme = AuthenticationSchemaNames.ApiKeyAuthentication)
//            .AddApiKeyAuthenticationSchema();

//        services.AddAuthorization();

//        services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

//        if (debugMode)
//            Console.WriteLine($"{nameof(AddSupportToolsServerApiKeyIdentity)} Finished");
//        return services;
//    }
//}