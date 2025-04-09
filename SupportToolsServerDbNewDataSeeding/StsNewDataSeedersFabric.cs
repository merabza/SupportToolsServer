using SupportToolsServerDbDataSeeding;

namespace SupportToolsServerDbNewDataSeeding;

public class StsNewDataSeedersFabric : StsDataSeedersFabric
{
    public StsNewDataSeedersFabric(string secretDataFolder, string dataSeedFolder, IStsDataSeederRepository repo) : base(secretDataFolder, dataSeedFolder,
        repo)
    {
    }
}