////Created by RepositoryCreatorFactoryCreator at 2/4/2025 7:31:10 PM

//using System;
//using Microsoft.Extensions.DependencyInjection;

//namespace LibSupportToolsServerRepositories;

//public sealed class SupportToolsServerRepositoryCreatorFactory : ISupportToolsServerRepositoryCreatorFactory
//{
//    private readonly IServiceProvider _services;

//    public SupportToolsServerRepositoryCreatorFactory(IServiceProvider services)
//    {
//        _services = services;
//    }

//    public ISupportToolsServerRepository GetSupportToolsServerRepository()
//    {
//        var scope = _services.CreateScope();
//        return scope.ServiceProvider.GetRequiredService<ISupportToolsServerRepository>();
//    }
//}

