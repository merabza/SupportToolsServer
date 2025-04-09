using Microsoft.Extensions.Logging;

namespace SupportToolsServerDbDataSeeding;

public /*open*/ class ProjectDataSeeder
{
    protected readonly ILogger Logger;
    private readonly StsDataSeedersFabric _dataSeedersFabric;
    private readonly bool _checkOnly;

    protected ProjectDataSeeder(ILogger logger, StsDataSeedersFabric dataSeedersFabric, bool checkOnly)
    {
        Logger = logger;
        _dataSeedersFabric = dataSeedersFabric;
        _checkOnly = checkOnly;
    }

    public virtual bool SeedData()
    {
        return SeedProjectData();
    }

    private bool SeedProjectData()
    {
        return true;
    }
}