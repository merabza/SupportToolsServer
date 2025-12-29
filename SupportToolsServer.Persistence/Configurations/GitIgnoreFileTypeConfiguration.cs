using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportToolsServer.Domain.GitIgnoreFileTypes;

namespace SupportToolsServer.Persistence.Configurations;

public class GitIgnoreFileTypeConfiguration : IEntityTypeConfiguration<GitIgnoreFileType>
{
    public void Configure(EntityTypeBuilder<GitIgnoreFileType> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(x => x.Id).HasConversion(gitIgnoreFileTypeId => gitIgnoreFileTypeId.Value,
            guidValue => new GitIgnoreFileTypeId(guidValue)).IsRequired();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Content).IsRequired().HasMaxLength(16384);
    }
}