using System;
using CliParameters;
using DbContextAnalyzer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using SupportToolsServerDbSeeder;
using SystemToolsShared;

ILogger<Program>? logger = null;
try
{
    Console.WriteLine("Seeds data in new database");
    var argParser =
        new ArgumentsParser<SeederParameters>(args, nameof(SupportToolsServerDbSeeder), null, "--CheckOnly");
    switch (argParser.Analysis())
    {
        case EParseResult.Ok: break;
        case EParseResult.Usage: return 1;
        case EParseResult.Error: return 2;
        default: throw new ArgumentOutOfRangeException();
    }

    var par = (SeederParameters?)argParser.Par;

    if (par is null)
    {
        StShared.WriteErrorLine("CreateProjectSeederCodeParameters is null", true);
        return 3;
    }

    if (string.IsNullOrWhiteSpace(par.ConnectionStringSeed))
    {
        StShared.WriteErrorLine("par.ConnectionStringSeed is empty", true);
        return 3;
    }

    var servicesCreator = new SeedDbServicesCreator(par.LogFolder, null, "SeedGrammarGeDb", par.ConnectionStringSeed);
    // ReSharper disable once using
    var serviceProvider = servicesCreator.CreateServiceProvider(LogEventLevel.Information);

    if (serviceProvider is null)
    {
        StShared.WriteErrorLine("serviceProvider does not created", true);
        return 4;
    }

    logger = serviceProvider.GetService<ILogger<Program>>();

    if (logger is null)
    {
        StShared.WriteErrorLine("logger is null", true);
        return 5;
    }

    if (string.IsNullOrWhiteSpace(par.SecretDataFolder))
    {
        StShared.WriteErrorLine("par.SecretDataFolder is empty", true);
        return 12;
    }

    if (string.IsNullOrWhiteSpace(par.JsonFolderName))
    {
        StShared.WriteErrorLine("par.JsonFolderName is empty", true);
        return 13;
    }

    var checkOnly = argParser.Switches.Contains("--CheckOnly");

    var seeder = new ProjectNewDataSeeder(carcassDataSeeder,
        new GrgNewDataSeedersFabric(userManager, roleManager, par.SecretDataFolder, par.JsonFolderName,
            grgDataSeederRepository), dataFixRepository, checkOnly);

    return seeder.SeedData() ? 0 : 1;
}
catch (Exception e)
{
    StShared.WriteException(e, true, logger);
    return 7;
}