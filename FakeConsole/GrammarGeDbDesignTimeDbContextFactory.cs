using SupportToolsServerDb;

namespace FakeHost;

//ეს კლასი საჭიროა იმისათვის, რომ შესაძლებელი გახდეს მიგრაციასთან მუშაობა.
//ანუ დეველოპერ ბაზის წაშლა და ახლიდან დაგენერირება, ან მიგრაციაში ცვლილებების გაკეთება
// ReSharper disable once UnusedType.Global
public sealed class SupportToolsServerDbDesignTimeDbContextFactory : DesignTimeDbContextFactory<SupportToolsServerDbContext>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsServerDbDesignTimeDbContextFactory() : base("SupportToolsServerDbMigration", "ConnectionStringSeed",
        @"D:\1WorkSecurity\SupportToolsServer\FakeHost.json")
    {
    }
}