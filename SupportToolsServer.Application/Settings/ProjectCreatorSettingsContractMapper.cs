using System.Collections.Generic;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.Settings;

internal static class ProjectCreatorSettingsContractMapper
{
    //ჩანაწერის ტიპი და სახელი რეესტრის შეცდომებში (ConcurrencyConflict, RecordWithNameNotFound):
    //"Settings ProjectCreator". ჩანაწერი ერთადერთია, ამიტომ სახელი ფიქსირებულია
    public const string EntityName = "Settings";
    public const string RecordName = "ProjectCreator";

    //მითითებები ბაზაში Id-ებით ინახება, კონტრაქტში კი სახელებით გადაიცემა
    public static StsProjectCreatorSettingsDataModel ToContractModel(this ProjectCreatorSettings projectCreatorSettings,
        IReadOnlyDictionary<ServerId, string> serverNames,
        IReadOnlyDictionary<DeploymentEnvironmentId, string> environmentNames,
        IReadOnlyDictionary<DatabaseServerConnectionId, string> connectionNames,
        IReadOnlyDictionary<FileStorageId, string> fileStorageNames,
        IReadOnlyDictionary<SmartSchemaId, string> smartSchemaNames)
    {
        return new StsProjectCreatorSettingsDataModel
        {
            IndentSize = projectCreatorSettings.IndentSize,
            FakeHostProjectName = projectCreatorSettings.FakeHostProjectName,
            ProjectsFolderPathReal = projectCreatorSettings.ProjectsFolderPathReal,
            SecretsFolderPathReal = projectCreatorSettings.SecretsFolderPathReal,
            ProductionServerName = serverNames.GetName(projectCreatorSettings.ProductionServerId),
            ProductionEnvironmentName = environmentNames.GetName(projectCreatorSettings.ProductionEnvironmentId),
            DeveloperDbConnectionName = connectionNames.GetName(projectCreatorSettings.DeveloperDbConnectionId),
            DatabaseExchangeFileStorageName =
                fileStorageNames.GetName(projectCreatorSettings.DatabaseExchangeFileStorageId),
            UseSmartSchema = smartSchemaNames.GetName(projectCreatorSettings.UseSmartSchemaId),
            Version = projectCreatorSettings.Version
        };
    }

    //ველი, რომელიც სერვერს მიმართავს, კონტრაქტის სახელით: მომხმარებელი სერვერის წაშლისას (409 RecordIsInUse). სანამ
    //ჩანაწერი შეიქმნება, მომხმარებელი არ არის. დანარჩენი მითითებული ტიპებიც ასევეა
    public static IEnumerable<string> GetUsages(this ProjectCreatorSettings? projectCreatorSettings,
        ServerId serverId)
    {
        return projectCreatorSettings is null
            ? []
            : ReferenceFields.Usages(nameof(ProjectCreatorSettings), serverId,
                (nameof(StsProjectCreatorSettingsDataModel.ProductionServerName),
                    projectCreatorSettings.ProductionServerId));
    }

    public static IEnumerable<string> GetUsages(this ProjectCreatorSettings? projectCreatorSettings,
        DeploymentEnvironmentId environmentId)
    {
        return projectCreatorSettings is null
            ? []
            : ReferenceFields.Usages(nameof(ProjectCreatorSettings), environmentId,
                (nameof(StsProjectCreatorSettingsDataModel.ProductionEnvironmentName),
                    projectCreatorSettings.ProductionEnvironmentId));
    }

    public static IEnumerable<string> GetUsages(this ProjectCreatorSettings? projectCreatorSettings,
        DatabaseServerConnectionId connectionId)
    {
        return projectCreatorSettings is null
            ? []
            : ReferenceFields.Usages(nameof(ProjectCreatorSettings), connectionId,
                (nameof(StsProjectCreatorSettingsDataModel.DeveloperDbConnectionName),
                    projectCreatorSettings.DeveloperDbConnectionId));
    }

    public static IEnumerable<string> GetUsages(this ProjectCreatorSettings? projectCreatorSettings,
        FileStorageId fileStorageId)
    {
        return projectCreatorSettings is null
            ? []
            : ReferenceFields.Usages(nameof(ProjectCreatorSettings), fileStorageId,
                (nameof(StsProjectCreatorSettingsDataModel.DatabaseExchangeFileStorageName),
                    projectCreatorSettings.DatabaseExchangeFileStorageId));
    }

    public static IEnumerable<string> GetUsages(this ProjectCreatorSettings? projectCreatorSettings,
        SmartSchemaId smartSchemaId)
    {
        return projectCreatorSettings is null
            ? []
            : ReferenceFields.Usages(nameof(ProjectCreatorSettings), smartSchemaId,
                (nameof(StsProjectCreatorSettingsDataModel.UseSmartSchema), projectCreatorSettings.UseSmartSchemaId));
    }
}
