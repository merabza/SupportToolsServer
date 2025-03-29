using CarcassDataSeeding;
using DatabaseToolsShared;

namespace SupportToolsServerDataSeeder;

public /*open*/ class ProjectDataSeedersFabric
{
    private readonly IDataSeederRepository _repo;
    protected readonly string DataSeedFolder;
    protected readonly RoleManager<AppRole> MyRoleManager;
    protected readonly string SecretDataFolder;

    protected ProjectDataSeedersFabric(string secretDataFolder, string dataSeedFolder, IDataSeederRepository repo)
    {
        SecretDataFolder = secretDataFolder;
        DataSeedFolder = dataSeedFolder;
        _repo = repo;
        _myUserManager = userManager;
        MyRoleManager = roleManager;
    }

    public virtual ITableDataSeeder CreateDataTypesSeeder()
    {
        return new DataTypesSeeder(DataSeedFolder, _repo);
    }

    public virtual ITableDataSeeder CreateAppClaimsSeeder()
    {
        return new AppClaimsSeeder(DataSeedFolder, _repo);
    }

    public virtual ITableDataSeeder CreateCrudRightTypesSeeder()
    {
        return new CrudRightTypesSeeder(DataSeedFolder, _repo);
    }

    public virtual ITableDataSeeder CreateManyToManyJoinsSeeder()
    {
        return new ManyToManyJoinsSeeder(SecretDataFolder, DataSeedFolder, _repo);
    }

    public virtual ITableDataSeeder CreateMenuGroupsSeeder()
    {
        return new MenuGroupsSeeder(DataSeedFolder, _repo);
    }

    public virtual ITableDataSeeder CreateMenuSeeder()
    {
        return new MenuSeeder(DataSeedFolder, _repo);
    }

    public virtual ITableDataSeeder CreateRolesSeeder()
    {
        return new RolesSeeder(MyRoleManager, SecretDataFolder, DataSeedFolder, _repo);
    }

    public virtual ITableDataSeeder CreateUsersSeeder()
    {
        return new UsersSeeder(_myUserManager, SecretDataFolder, DataSeedFolder, _repo);
    }
}