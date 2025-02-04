//Created by RepositoryCreatorFabricCreator at 2/4/2025 7:31:10 PM

using System;
using Microsoft.Extensions.DependencyInjection;

namespace LibSupportToolsServerRepositories;

public sealed class SupportToolsServerRepositoryCreatorFabric : ISupportToolsServerRepositoryCreatorFabric
{
    private readonly IServiceProvider _services;

    public SupportToolsServerRepositoryCreatorFabric(IServiceProvider services)
    {
        _services = services;
    }

    public ISupportToolsServerRepository GetSupportToolsServerRepository()
    {
        IServiceScope scope = _services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ISupportToolsServerRepository>();
    }
}