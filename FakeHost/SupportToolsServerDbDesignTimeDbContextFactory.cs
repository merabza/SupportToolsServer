//Created by FakeProjectDesignTimeDbContextFactoryCreator at 4/14/2025 11:57:44 PM

using SupportToolsServer.Persistence;

namespace FakeHost;

//ეს კლასი საჭიროა იმისათვის, რომ შესაძლებელი გახდეს მიგრაციასთან მუშაობა.
//ანუ დეველოპერ ბაზის წაშლა და ახლიდან დაგენერირება, ან მიგრაციაში ცვლილებების გაკეთება
// ReSharper disable once UnusedType.Global
public sealed class
    SupportToolsServerDbDesignTimeDbContextFactory : DesignTimeDbContextFactory<SupportToolsServerDbContext>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsServerDbDesignTimeDbContextFactory() : base("SupportToolsServer.DbMigration",
        "ConnectionStringSeed", @"D:\1WorkSecurity\SupportToolsServer\FakeHost.json")
    {
    }
}