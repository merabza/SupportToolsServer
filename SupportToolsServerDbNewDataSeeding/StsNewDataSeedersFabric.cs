using SupportToolsServerDbDataSeeding;

namespace SupportToolsServerDbNewDataSeeding;

public sealed class StsNewDataSeedersFabric : StsDataSeedersFabric
{
    public StsNewDataSeedersFabric(string secretDataFolder, string dataSeedFolder, IStsDataSeederRepository repo) : base(secretDataFolder, dataSeedFolder,
        repo)
    {
    }
}