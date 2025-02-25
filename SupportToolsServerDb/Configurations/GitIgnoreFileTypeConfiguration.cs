using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportToolsServerDb.Models;
using SystemToolsShared;

namespace SupportToolsServerDb.Configurations;

public class GitIgnoreFileTypeConfiguration : IEntityTypeConfiguration<GitIgnoreFileType>
{
    public void Configure(EntityTypeBuilder<GitIgnoreFileType> builder)
    {
        var tableName = nameof(GitIgnoreFileType).Pluralize();

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Name).HasDatabaseName(tableName.CreateIndexName(true, nameof(GitIgnoreFileType.Name)))
            .IsUnique();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Content).IsRequired().HasMaxLength(16384);
    }
}