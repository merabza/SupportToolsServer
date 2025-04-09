//Created by DataSeederRepositoryCreator at 4/7/2025 12:05:19 AM

using DatabaseToolsShared;
using Microsoft.Extensions.Logging;
using SupportToolsServerDb;

namespace SupportToolsServerDbDataSeeding;

public sealed class StsDataSeederRepository : DataSeederRepository, IStsDataSeederRepository
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public StsDataSeederRepository(SupportToolsServerDbContext ctx, ILogger<StsDataSeederRepository> logger) : base(ctx,
        logger)
    {
    }
}