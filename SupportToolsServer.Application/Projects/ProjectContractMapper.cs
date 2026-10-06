using System;
using System.Collections.Generic;
using System.Linq;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.Projects;

internal static class ProjectContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "Project";

    //მითითებები ბაზაში Id-ებით ინახება, კონტრაქტში კი სახელებით გადაიცემა. სიები სიმრავლეებია, ამიტომ სახელით ლაგდება,
    //endpoint-ები და route კლასები Name-ით, ServerInfo-ები კი სერვერისა და გარემოს სახელებით: კლიენტის ჰეში რიგზე არ
    //უნდა იყოს დამოკიდებული
    public static StsProjectDataModel ToContractModel(this Project project, ProjectReferenceNames names)
    {
        return new StsProjectDataModel
        {
            Name = project.Name,
            ProjectType = project.ProjectType,
            ProjectGroupName = project.ProjectGroupName,
            ProjectDescription = project.ProjectDescription,
            MajorVersion = project.MajorVersion,
            MinorVersion = project.MinorVersion,
            UseAlternativeWebAgent = project.UseAlternativeWebAgent,
            EditorConfigPatternName = names.EditorConfigFileTypes.GetName(project.EditorConfigFileTypeId),
            MainProjectName = project.MainProjectName,
            ApiContractsProjectName = project.ApiContractsProjectName,
            SpaProjectName = project.SpaProjectName,
            DbContextName = project.DbContextName,
            ProjectShortPrefix = project.ProjectShortPrefix,
            ScaffoldSeederProjectName = project.ScaffoldSeederProjectName,
            DbContextProjectName = project.DbContextProjectName,
            NewDataSeedingClassLibProjectName = project.NewDataSeedingClassLibProjectName,
            ProgramArchiveDateMask = project.ProgramArchiveDateMask,
            ProgramArchiveExtension = project.ProgramArchiveExtension,
            ParametersFileDateMask = project.ParametersFileDateMask,
            ParametersFileExtension = project.ParametersFileExtension,
            ProjectFolderName = project.ProjectFolderName,
            SolutionFileName = project.SolutionFileName,
            ProjectSecurityFolderPath = project.ProjectSecurityFolderPath,
            MigrationStartupProjectFilePath = project.MigrationStartupProjectFilePath,
            MigrationProjectFilePath = project.MigrationProjectFilePath,
            DataSeederRulesByTableStartupProjectFilePath = project.DataSeederRulesByTableStartupProjectFilePath,
            OldDataConvertorForDataSeeder = project.OldDataConvertorForDataSeeder,
            SeedProjectFilePath = project.SeedProjectFilePath,
            SeedProjectParametersFilePath = project.SeedProjectParametersFilePath,
            ExcludesRulesParametersFilePath = project.ExcludesRulesParametersFilePath,
            AppSetEnKeysJsonFileName = project.AppSetEnKeysJsonFileName,
            MigrationSqlFilesFolder = project.MigrationSqlFilesFolder,
            PrepareProdCopyDatabaseProjectFilePath = project.PrepareProdCopyDatabaseProjectFilePath,
            PrepareProdCopyDatabaseProjectParametersFilePath = project.PrepareProdCopyDatabaseProjectParametersFilePath,
            PairedDbObjectsResultFileName = project.PairedDbObjectsResultFileName,
            KeyGuidPart = project.KeyGuidPart,
            DevDatabaseParameters = project.DevDatabaseParameters?.ToContractModel(names.DatabaseServerConnections,
                names.SmartSchemas, names.FileStorages),
            ProdCopyDatabaseParameters = project.ProdCopyDatabaseParameters?.ToContractModel(
                names.DatabaseServerConnections, names.SmartSchemas, names.FileStorages),
            GitProjectNames = GitNames(project, EProjectGitRepoKind.Main, names),
            ScaffoldSeederGitProjectNames = GitNames(project, EProjectGitRepoKind.ScaffoldSeed, names),
            FrontNpmPackageNames = Sorted(project.NpmPackages.Select(x => names.NpmPackages[x.NpmPackageId])),
            RedundantFileNames = Sorted(project.RedundantFiles.Select(x => x.FileName)),
            AllowToolsList = Sorted(project.AllowedTools.Select(x => x.ToolName)),
            Endpoints =
            [
                .. project.Endpoints.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x =>
                    new StsProjectEndpointDataModel
                    {
                        Name = x.Name,
                        EndpointName = x.EndpointName,
                        EndpointRoute = x.EndpointRoute,
                        RequireAuthorization = x.RequireAuthorization,
                        HttpMethod = x.HttpMethod,
                        EndpointType = x.EndpointType,
                        ReturnType = x.ReturnType,
                        SendMessageToCurrentUser = x.SendMessageToCurrentUser
                    })
            ],
            RouteClasses =
            [
                .. project.RouteClasses.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x =>
                    new StsProjectRouteClassDataModel
                    {
                        Name = x.Name, Root = x.Root, ApiVersion = x.ApiVersion, Base = x.Base
                    })
            ],
            ServerInfos = project.ServerInfos.ToContractModels(names),
            Version = project.Version
        };
    }

    //პროექტები, რომლებიც მითითებულ ჩანაწერს იყენებს, "Project <სახელი>" ფორმით, სახელის რიგით, თითო პროექტი ერთხელ:
    //მომხმარებლები ჩანაწერის წაშლისას (409 RecordIsInUse)
    public static IEnumerable<string> GetUsages(this IEnumerable<Project> projects, GitRepoId gitRepoId)
    {
        return projects.Usages(x => x.GitRepos.Any(y => y.GitRepoId.Equals(gitRepoId)));
    }

    public static IEnumerable<string> GetUsages(this IEnumerable<Project> projects, NpmPackageId npmPackageId)
    {
        return projects.Usages(x => x.NpmPackages.Any(y => y.NpmPackageId.Equals(npmPackageId)));
    }

    public static IEnumerable<string> GetUsages(this IEnumerable<Project> projects,
        EditorConfigFileTypeId editorConfigFileTypeId)
    {
        return projects.Usages(x => editorConfigFileTypeId.Equals(x.EditorConfigFileTypeId));
    }

    //ServerInfo-ები, რომლებიც მითითებულ ჩანაწერს იყენებს, "Project <სახელი> / <სერვერი>|<გარემო>" ფორმით: პროექტები
    //სახელის რიგით, თითო პროექტში ServerInfo-ები სერვერისა და გარემოს სახელების რიგით
    public static IEnumerable<string> GetUsages(this IEnumerable<Project> projects, ServerId serverId,
        ServerInfoKeyNames keyNames)
    {
        return projects.Usages(_ => false, x => serverId.Equals(x.ServerId), keyNames);
    }

    public static IEnumerable<string> GetUsages(this IEnumerable<Project> projects,
        DeploymentEnvironmentId environmentId, ServerInfoKeyNames keyNames)
    {
        return projects.Usages(_ => false, x => environmentId.Equals(x.EnvironmentId), keyNames);
    }

    public static IEnumerable<string> GetUsages(this IEnumerable<Project> projects, ApiClientId apiClientId,
        ServerInfoKeyNames keyNames)
    {
        return projects.Usages(_ => false, x => apiClientId.Equals(x.WebAgentForCheckId), keyNames);
    }

    //ბაზის კავშირს, ჭკვიან სქემასა და ფაილსაცავს პროექტის ორივე ბაზის პარამეტრი მიმართავს ("Project <სახელი>") და
    //ServerInfo-ების ორივე ბაზის პარამეტრიც ("Project <სახელი> / <სერვერი>|<გარემო>")
    public static IEnumerable<string> GetUsages(this IEnumerable<Project> projects,
        DatabaseServerConnectionId connectionId, ServerInfoKeyNames keyNames)
    {
        return projects.Usages(
            x => x.DevDatabaseParameters.Uses(connectionId) || x.ProdCopyDatabaseParameters.Uses(connectionId),
            x => x.CurrentDatabaseParameters.Uses(connectionId) || x.NewDatabaseParameters.Uses(connectionId),
            keyNames);
    }

    public static IEnumerable<string> GetUsages(this IEnumerable<Project> projects, SmartSchemaId smartSchemaId,
        ServerInfoKeyNames keyNames)
    {
        return projects.Usages(
            x => x.DevDatabaseParameters.Uses(smartSchemaId) || x.ProdCopyDatabaseParameters.Uses(smartSchemaId),
            x => x.CurrentDatabaseParameters.Uses(smartSchemaId) || x.NewDatabaseParameters.Uses(smartSchemaId),
            keyNames);
    }

    public static IEnumerable<string> GetUsages(this IEnumerable<Project> projects, FileStorageId fileStorageId,
        ServerInfoKeyNames keyNames)
    {
        return projects.Usages(
            x => x.DevDatabaseParameters.Uses(fileStorageId) || x.ProdCopyDatabaseParameters.Uses(fileStorageId),
            x => x.CurrentDatabaseParameters.Uses(fileStorageId) || x.NewDatabaseParameters.Uses(fileStorageId),
            keyNames);
    }

    private static IEnumerable<string> Usages(this IEnumerable<Project> projects, Func<Project, bool> usesRecord)
    {
        return projects.Where(usesRecord).Select(x => x.Name).Order(StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{EntityName} {x}");
    }

    //პროექტის სახელის რიგით: ჯერ თვითონ პროექტი, თუ მისი ველები ჩანაწერს იყენებს, მერე მისი ServerInfo-ები, რომლებიც
    //ჩანაწერს იყენებს
    private static IEnumerable<string> Usages(this IEnumerable<Project> projects, Func<Project, bool> projectUsesRecord,
        Func<ServerInfo, bool> serverInfoUsesRecord, ServerInfoKeyNames keyNames)
    {
        foreach (Project project in projects.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (projectUsesRecord(project))
            {
                yield return $"{EntityName} {project.Name}";
            }

            foreach (string key in keyNames.SortedKeys(project.ServerInfos.Where(serverInfoUsesRecord)))
            {
                yield return $"{EntityName} {project.Name} / {key}";
            }
        }
    }

    private static List<string> GitNames(Project project, EProjectGitRepoKind kind, ProjectReferenceNames names)
    {
        return Sorted(project.GitRepos.Where(x => x.Kind == kind).Select(x => names.GitRepos[x.GitRepoId]));
    }

    private static List<string> Sorted(IEnumerable<string> values)
    {
        return [.. values.Order(StringComparer.OrdinalIgnoreCase)];
    }
}
