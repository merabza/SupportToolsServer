//Created by DbContextClassCreator at 2/4/2025 7:31:10 PM

using SupportToolsServerDb.Models;
using SystemToolsShared;
using Microsoft.EntityFrameworkCore;

namespace SupportToolsServerDb;

public sealed class SupportToolsServerDbContext : DbContext
{
    public SupportToolsServerDbContext(DbContextOptions options, bool isDesignTime) : base(options)
    {
    }

    public SupportToolsServerDbContext(DbContextOptions<SupportToolsServerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestModel>(entity =>
        {
            string tableName = nameof(TestModel).Pluralize();
            entity.HasKey(e => e.TestId);
            entity.ToTable(tableName.UnCapitalize());
            entity.HasIndex(e => e.TestName)
                .HasDatabaseName($"IX_{tableName}_{nameof(TestModel.TestName).UnCapitalize()}").IsUnique();
            entity.Property(e => e.TestId).HasColumnName(nameof(TestModel.TestId).UnCapitalize());
            entity.Property(e => e.TestName).HasColumnName(nameof(TestModel.TestName).UnCapitalize()).HasMaxLength(50);
        });
    }
}