using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportToolsServerDb.Models;

namespace SupportToolsServerDb.Configurations;

public sealed class ApiKeyByRemoteIpAddressConfiguration : IEntityTypeConfiguration<ApiKeyByRemoteIpAddress>
{
    public void Configure(EntityTypeBuilder<ApiKeyByRemoteIpAddress> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.ApiKey, e.RemoteIpAddress }).IsUnique();

        builder.Property(e => e.ApiKey).IsRequired().HasMaxLength(50);
        builder.Property(e => e.RemoteIpAddress).IsRequired().HasMaxLength(50);
    }
}