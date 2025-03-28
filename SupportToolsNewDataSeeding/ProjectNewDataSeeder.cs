namespace SupportToolsNewDataSeeding;

public sealed class ProjectNewDataSeeder : ProjectDataSeeder
{
    private readonly IDataFixRepository _dataFixRepository;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectNewDataSeeder(ILogger<CarcassDataSeeder> logger, DataSeedersFabric dataSeedersFabric,
        IDataFixRepository dataFixRepository, bool checkOnly) : base(logger, dataSeedersFabric, checkOnly)
    {
        _dataFixRepository = dataFixRepository;
    }

    protected override bool SeedProjectSpecificData()
    {
        Logger.LogInformation("Seed Agr Project New Data Seeder Started");

        if (!base.SeedProjectSpecificData())
            return false;

        var afterSeeDataFixer = new DataFixer(Logger, _dataFixRepository);

        Logger.LogInformation("Running DataFixer");
        return afterSeeDataFixer.Run();
    }
}