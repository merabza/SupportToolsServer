namespace SupportToolsServerDbDataSeeding;

public /*open*/ class StsDataSeedersFabric
{
    protected IStsDataSeederRepository Repo { get; }

    protected StsDataSeedersFabric(string secretDataFolder, string dataSeedFolder, IStsDataSeederRepository repo)
    {
        Repo = repo;
    }
}