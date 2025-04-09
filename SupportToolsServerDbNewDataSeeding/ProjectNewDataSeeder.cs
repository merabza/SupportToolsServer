using Microsoft.Extensions.Logging;
using SupportToolsNewDataSeeding;
using SupportToolsServerDbDataSeeding;

namespace SupportToolsServerDbNewDataSeeding;

public sealed class ProjectNewDataSeeder : ProjectDataSeeder
{
    private readonly IDataFixRepository _dataFixRepository;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectNewDataSeeder(ILogger logger, StsNewDataSeedersFabric dataSeedersFabric,
        IDataFixRepository dataFixRepository, bool checkOnly) : base(logger, dataSeedersFabric, checkOnly)
    {
        _dataFixRepository = dataFixRepository;
    }

    public override bool SeedData()
    {
        if (!base.SeedData())
            return false;

        Logger.LogInformation("Seed Sts Project New Data Seeder Started");

        var afterSeeDataFixer = new DataFixer(Logger, _dataFixRepository);

        Logger.LogInformation("Running DataFixer");
        return afterSeeDataFixer.Run();
    }
}