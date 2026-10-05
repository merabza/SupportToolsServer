using System.Collections.Generic;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.Settings;

internal static class GlobalSettingsContractMapper
{
    //ჩანაწერის ტიპი და სახელი რეესტრის შეცდომებში (ConcurrencyConflict, RecordWithNameNotFound): "Settings Global".
    //ჩანაწერი ერთადერთია, ამიტომ სახელი ფიქსირებულია
    public const string EntityName = "Settings";
    public const string RecordName = "Global";

    //მითითებები ბაზაში Id-ებით ინახება, კონტრაქტში კი სახელებით გადაიცემა
    public static StsGlobalSettingsDataModel ToContractModel(this GlobalSettings globalSettings,
        IReadOnlyDictionary<FileStorageId, string> fileStorageNames,
        IReadOnlyDictionary<SmartSchemaId, string> smartSchemaNames,
        IReadOnlyDictionary<ApiClientId, string> apiClientNames)
    {
        DatabasesBackupFilesExchange exchange = globalSettings.DatabasesBackupFilesExchange;
        return new StsGlobalSettingsDataModel
        {
            ServiceDescriptionSignature = globalSettings.ServiceDescriptionSignature,
            UploadTempExtension = globalSettings.UploadTempExtension,
            ProgramArchiveDateMask = globalSettings.ProgramArchiveDateMask,
            ProgramArchiveExtension = globalSettings.ProgramArchiveExtension,
            ParametersFileDateMask = globalSettings.ParametersFileDateMask,
            ParametersFileExtension = globalSettings.ParametersFileExtension,
            MediatRLicenseKey = globalSettings.MediatRLicenseKey,
            FileStorageNameForExchange = fileStorageNames.GetName(globalSettings.FileStorageForExchangeId),
            SmartSchemaNameForExchange = smartSchemaNames.GetName(globalSettings.SmartSchemaForExchangeId),
            SmartSchemaNameForLocal = smartSchemaNames.GetName(globalSettings.SmartSchemaForLocalId),
            LocalPackageManagerWebApiClientName =
                apiClientNames.GetName(globalSettings.LocalPackageManagerWebApiClientId),
            DatabasesBackupFilesExchange = new StsDatabasesBackupFilesExchangeDataModel
            {
                DownloadTempExtension = exchange.DownloadTempExtension,
                UploadTempExtension = exchange.UploadTempExtension,
                ExchangeFileStorageName = fileStorageNames.GetName(exchange.ExchangeFileStorageId),
                ExchangeSmartSchemaName = smartSchemaNames.GetName(exchange.ExchangeSmartSchemaId),
                LocalSmartSchemaName = smartSchemaNames.GetName(exchange.LocalSmartSchemaId)
            },
            Version = globalSettings.Version
        };
    }

    //DatabasesBackupFilesExchange-ის ველის სახელი, როგორც კონტრაქტში: DatabasesBackupFilesExchange.<ველი>
    public static string ExchangeFieldName(string fieldName)
    {
        return $"{nameof(StsGlobalSettingsDataModel.DatabasesBackupFilesExchange)}.{fieldName}";
    }

    //ველები, რომლებიც ფაილსაცავს მიმართავს, კონტრაქტის სახელებითა და რიგით: მომხმარებლები ფაილსაცავის წაშლისას
    //(409 RecordIsInUse). სანამ ჩანაწერი შეიქმნება, მომხმარებელი არ არის
    public static IEnumerable<string> GetUsages(this GlobalSettings? globalSettings, FileStorageId fileStorageId)
    {
        return globalSettings is null
            ? []
            : ReferenceFields.Usages(nameof(GlobalSettings), fileStorageId,
                (nameof(StsGlobalSettingsDataModel.FileStorageNameForExchange),
                    globalSettings.FileStorageForExchangeId),
                (ExchangeFieldName(nameof(StsDatabasesBackupFilesExchangeDataModel.ExchangeFileStorageName)),
                    globalSettings.DatabasesBackupFilesExchange.ExchangeFileStorageId));
    }

    //ველები, რომლებიც ჭკვიან სქემას მიმართავს, ფაილსაცავის მსგავსად
    public static IEnumerable<string> GetUsages(this GlobalSettings? globalSettings, SmartSchemaId smartSchemaId)
    {
        return globalSettings is null
            ? []
            : ReferenceFields.Usages(nameof(GlobalSettings), smartSchemaId,
                (nameof(StsGlobalSettingsDataModel.SmartSchemaNameForExchange),
                    globalSettings.SmartSchemaForExchangeId),
                (nameof(StsGlobalSettingsDataModel.SmartSchemaNameForLocal), globalSettings.SmartSchemaForLocalId),
                (ExchangeFieldName(nameof(StsDatabasesBackupFilesExchangeDataModel.ExchangeSmartSchemaName)),
                    globalSettings.DatabasesBackupFilesExchange.ExchangeSmartSchemaId),
                (ExchangeFieldName(nameof(StsDatabasesBackupFilesExchangeDataModel.LocalSmartSchemaName)),
                    globalSettings.DatabasesBackupFilesExchange.LocalSmartSchemaId));
    }

    //ველი, რომელიც ApiClient-ს მიმართავს, ფაილსაცავის მსგავსად
    public static IEnumerable<string> GetUsages(this GlobalSettings? globalSettings, ApiClientId apiClientId)
    {
        return globalSettings is null
            ? []
            : ReferenceFields.Usages(nameof(GlobalSettings), apiClientId,
                (nameof(StsGlobalSettingsDataModel.LocalPackageManagerWebApiClientName),
                    globalSettings.LocalPackageManagerWebApiClientId));
    }
}
