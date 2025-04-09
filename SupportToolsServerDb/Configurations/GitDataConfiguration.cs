using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportToolsServerDb.Models;

namespace SupportToolsServerDb.Configurations;

public sealed class GitDataConfiguration : IEntityTypeConfiguration<GitData>
{
    public void Configure(EntityTypeBuilder<GitData> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Name).IsUnique();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(50);
        builder.Property(e => e.GitAddress).IsRequired().HasMaxLength(128);
        builder.Property(e => e.GitIgnoreFileTypeId).IsRequired();

        builder.HasOne(h => h.GitIgnoreFileTypeNavigation).WithMany(w => w.GitData)
            .HasForeignKey(f => f.GitIgnoreFileTypeId);
    }
}