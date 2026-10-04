using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;
using SupportToolsServer.Application.Environments.GetEnvironmentByName;
using SupportToolsServer.Application.GitRepos.GetGitRepos;
using SupportToolsServer.Application.NpmPackages.GetNpmPackageByName;
using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Runs the real Program.cs (its services and its middleware order) on a TestServer.
//The environment is Production, so the User Secrets of the developer are never read.
//Program.cs reads the connection string and the Serilog section before Build, so they come as settings,
//which WebApplicationFactory passes as command line arguments. The rest is read later and comes from an in-memory source.
//The factory passes the parent keys of a setting as well, with empty values: Serilog takes the empty
//Serilog:WriteTo:1 for a sink without a name and skips it, so the test hosts write no log file (only to the console).
//The connection string is valid only in format: the handlers of GET gitrepos, GET environments/{key} and
//GET npmpackages/{key} are stubs, so no database is opened. The last two return the name they received, which shows
//how the route key was decoded
public sealed class SupportToolsServerHostFactory : WebApplicationFactory<Program>
{
    public const string ValidApiKey = "valid-test-key";
    public const string AnyAddressApiKey = "any-address-test-key";
    public const string OtherAddressApiKey = "other-address-test-key";
    public const string OtherAddress = "10.99.99.99";

    private readonly TempFolder _tempFolder = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);
        builder.UseSetting("Data:SupportToolsServerDatabase:ConnectionString",
            @"Server=(localdb)\MSSQLLocalDB;Database=SupportToolsServerAuthenticationTests;Trusted_Connection=True");
        builder.UseSetting("Serilog:WriteTo:1:Args:path", _tempFolder.Combine("Logs", "SupportToolsServer.log"));

        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["AppOptions:WorkFolder"] = _tempFolder.Combine("Work"),
                ["ApiKeys:AppSettingsByApiKey:0:ApiKey"] = ValidApiKey,
                ["ApiKeys:AppSettingsByApiKey:0:RemoteIpAddress"] = ClientAddressStartupFilter.DefaultClientAddress,
                ["ApiKeys:AppSettingsByApiKey:1:ApiKey"] = AnyAddressApiKey,
                ["ApiKeys:AppSettingsByApiKey:1:RemoteIpAddress"] = "*",
                ["ApiKeys:AppSettingsByApiKey:2:ApiKey"] = OtherAddressApiKey,
                ["ApiKeys:AppSettingsByApiKey:2:RemoteIpAddress"] = OtherAddress
            }));

        builder.ConfigureTestServices(services =>
        {
            services.AddTransient<IStartupFilter, ClientAddressStartupFilter>();
            services.AddScoped(_ => HandlerMocks
                .Query<GetGitReposQuery, List<StsGitDataModel>>(Result.Success(new List<StsGitDataModel>())).Object);
            services.AddScoped(_ => EnvironmentNameEchoHandler());
            services.AddScoped(_ => NpmPackageNameEchoHandler());
        });
    }

    private static IQueryHandler<GetEnvironmentByNameQuery, StsEnvironmentDataModel> EnvironmentNameEchoHandler()
    {
        var handler = new Mock<IQueryHandler<GetEnvironmentByNameQuery, StsEnvironmentDataModel>>();
        handler.Setup(h => h.Handle(It.IsAny<GetEnvironmentByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetEnvironmentByNameQuery query, CancellationToken _) =>
                Result.Success(new StsEnvironmentDataModel { Name = query.Name }));
        return handler.Object;
    }

    private static IQueryHandler<GetNpmPackageByNameQuery, StsNpmPackageDataModel> NpmPackageNameEchoHandler()
    {
        var handler = new Mock<IQueryHandler<GetNpmPackageByNameQuery, StsNpmPackageDataModel>>();
        handler.Setup(h => h.Handle(It.IsAny<GetNpmPackageByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetNpmPackageByNameQuery query, CancellationToken _) =>
                Result.Success(new StsNpmPackageDataModel { Name = query.Name }));
        return handler.Object;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        //Program.cs closes the static Serilog logger only after the host has stopped, on its own thread
        Log.CloseAndFlush();
        _tempFolder.Dispose();
    }
}
