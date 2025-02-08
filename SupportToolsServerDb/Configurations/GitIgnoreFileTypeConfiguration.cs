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
        builder.ToTable(tableName.UnCapitalize());

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Name).HasDatabaseName(tableName.CreateIndexName(true, nameof(GitIgnoreFileType.Name)))
            .IsUnique();
        builder.Property(e => e.Id).HasColumnName(nameof(GitIgnoreFileType.Id));
        builder.Property(e => e.Name).IsRequired().HasColumnName(nameof(GitIgnoreFileType.Name))
            .HasMaxLength(50);
        builder.Property(e => e.Content).IsRequired().HasColumnName(nameof(GitIgnoreFileType.Content))
            .HasMaxLength(16384);
    }
}