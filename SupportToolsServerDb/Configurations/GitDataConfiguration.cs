using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportToolsServerDb.Models;
using SystemToolsShared;

namespace SupportToolsServerDb.Configurations;

public class GitDataConfiguration : IEntityTypeConfiguration<GitData>
{
    public void Configure(EntityTypeBuilder<GitData> builder)
    {
        var tableName = nameof(GitData).Pluralize();
        builder.ToTable(tableName.UnCapitalize());

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Name).HasDatabaseName(tableName.CreateIndexName(true, nameof(GitData.Name))).IsUnique();
        builder.Property(e => e.Id).HasColumnName(nameof(GitData.Id));
        builder.Property(e => e.Name).IsRequired().HasColumnName(nameof(GitData.Name)).HasMaxLength(50);
        builder.Property(e => e.GitAddress).IsRequired().HasColumnName(nameof(GitData.GitAddress)).HasMaxLength(128);
        builder.Property(e => e.GitIgnoreFileTypeId).IsRequired().HasColumnName(nameof(GitData.GitIgnoreFileTypeId));

        builder.HasOne(h => h.GitIgnoreFileTypeNavigation).WithMany(w => w.GitData)
            .HasForeignKey(f => f.GitIgnoreFileTypeId);
    }
}