using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.Settings;

//გლობალური პარამეტრების DatabasesBackupFilesExchange ნაწილის წესები. სიგრძეები GlobalSettingsConfiguration-ის
//HasMaxLength-ს ემთხვევა, მითითებული ჩანაწერების სახელებისა კი FileStorage-ისა და SmartSchema-ს სახელების სიგრძეს.
//ვალიდატორი public-ია და პარამეტრის გარეშე, რადგან AddFluentValidation ყველა public ვალიდატორს DI-ში არეგისტრირებს;
//ამიტომ შეტყობინებები ველს ნაწილის სახელით ასახელებს (DatabasesBackupFilesExchange.UploadTempExtension)
public sealed class DatabasesBackupFilesExchangeModelValidator :
    AbstractValidator<StsDatabasesBackupFilesExchangeDataModel>
{
    public DatabasesBackupFilesExchangeModelValidator()
    {
        RuleFor(x => x.DownloadTempExtension).OptionalWithMaxLength(
            _ => GlobalSettingsContractMapper.ExchangeFieldName(
                nameof(StsDatabasesBackupFilesExchangeDataModel.DownloadTempExtension)),
            DatabasesBackupFilesExchange.DownloadTempExtensionMaxLength);

        RuleFor(x => x.UploadTempExtension).OptionalWithMaxLength(
            _ => GlobalSettingsContractMapper.ExchangeFieldName(
                nameof(StsDatabasesBackupFilesExchangeDataModel.UploadTempExtension)),
            DatabasesBackupFilesExchange.UploadTempExtensionMaxLength);

        RuleFor(x => x.ExchangeFileStorageName).OptionalWithMaxLength(
            _ => GlobalSettingsContractMapper.ExchangeFieldName(
                nameof(StsDatabasesBackupFilesExchangeDataModel.ExchangeFileStorageName)), FileStorage.NameMaxLength);

        RuleFor(x => x.ExchangeSmartSchemaName).OptionalWithMaxLength(
            _ => GlobalSettingsContractMapper.ExchangeFieldName(
                nameof(StsDatabasesBackupFilesExchangeDataModel.ExchangeSmartSchemaName)), SmartSchema.NameMaxLength);

        RuleFor(x => x.LocalSmartSchemaName).OptionalWithMaxLength(
            _ => GlobalSettingsContractMapper.ExchangeFieldName(
                nameof(StsDatabasesBackupFilesExchangeDataModel.LocalSmartSchemaName)), SmartSchema.NameMaxLength);
    }
}
