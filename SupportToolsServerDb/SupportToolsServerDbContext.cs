using DatabaseToolsShared;
using Microsoft.EntityFrameworkCore;
using SupportToolsServerDb.Models;

namespace SupportToolsServerDb;

public sealed class SupportToolsServerDbContext : DbContext
{
    public SupportToolsServerDbContext(DbContextOptions<SupportToolsServerDbContext> options, bool isDesignTime) :
        base(options)
    {
        //Console.WriteLine("SupportToolsServerDbContext Constructor 2...");
    }

    public SupportToolsServerDbContext(DbContextOptions<SupportToolsServerDbContext> options, int int1) : base(options)
    {
        //Console.WriteLine("SupportToolsServerDbContext Constructor 3...");
    }

    public SupportToolsServerDbContext(DbContextOptions<SupportToolsServerDbContext> options) : base(options)
    {
        //Console.WriteLine("SupportToolsServerDbContext Constructor 4...");
    }

    //ბაზაში არსებული ცხრილები წარმოდგენილი DbSet-ების სახით
    public DbSet<GitData> GitData => Set<GitData>();
    public DbSet<GitIgnoreFileType> GitIgnoreFileTypes => Set<GitIgnoreFileType>();
    public DbSet<ApiKeyByRemoteIpAddress> ApiKeysByRemoteIpAddresses => Set<ApiKeyByRemoteIpAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Console.WriteLine("AppGrammarGeDbContext OnModelCreating Start...");

        base.OnModelCreating(modelBuilder);

        //Console.WriteLine("AppGrammarGeDbContext OnModelCreating Pass 1...");

        modelBuilder.ApplyConfigurationsFromAssembly(AssemblyReference.Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new DatabaseEntitiesDefaultConvention());
    }
}