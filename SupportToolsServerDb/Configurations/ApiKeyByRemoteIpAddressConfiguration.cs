using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportToolsServerDb.Models;
using SystemToolsShared;

namespace SupportToolsServerDb.Configurations;

public class ApiKeyByRemoteIpAddressConfiguration : IEntityTypeConfiguration<ApiKeyByRemoteIpAddress>
{
    public void Configure(EntityTypeBuilder<ApiKeyByRemoteIpAddress> builder)
    {
        var tableName = nameof(ApiKeyByRemoteIpAddress).Pluralize();

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.ApiKey, e.RemoteIpAddress }).IsUnique();

        builder.Property(e => e.ApiKey).IsRequired().HasMaxLength(50);
        builder.Property(e => e.RemoteIpAddress).IsRequired().HasMaxLength(50);
    }
}