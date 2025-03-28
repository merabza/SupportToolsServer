//Created by ServiceCreatorCreator at 3/27/2025 12:24:23 AM

using Microsoft.Extensions.DependencyInjection;
using SystemToolsShared;

namespace SupportToolsServerDbSeeder;

public sealed class SeedDbServicesCreator : ServicesCreator
{
    private readonly string _connectionString;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SeedDbServicesCreator(string? logFolder, string? logFileName, string appName, string connectionString) :
        base(logFolder, logFileName, appName)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        //identity
        services.AddScoped<IUserStore<AppUser>, MyUserStore>();
        services.AddScoped<IUserPasswordStore<AppUser>, MyUserStore>();
        services.AddScoped<IUserEmailStore<AppUser>, MyUserStore>();
        services.AddScoped<IUserRoleStore<AppUser>, MyUserStore>();
        services.AddScoped<IRoleStore<AppRole>, MyUserStore>();

        services.AddScoped<IIdentityRepository, IdentityRepository>();

        services.AddScoped<IGrgDataSeederRepository, GrgDataSeederRepository>();

        services.AddScoped<IDataFixRepository, DataFixRepository>();

        services.AddDbContext<CarcassDbContext>(options => options.UseSqlServer(_connectionString));

        services.AddDbContext<GrammarGeDbContext>(options => options.UseSqlServer(_connectionString));

        services.AddIdentity<AppUser, AppRole>(options =>
        {
            options.Password.RequiredLength = 3;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireDigit = false;
        }).AddDefaultTokenProviders();
    }
}