using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.Settings;

//გლობალური პარამეტრების წესები. სიგრძეები GlobalSettingsConfiguration-ის HasMaxLength-ს ემთხვევა, მითითებული
//ჩანაწერების სახელებისა კი FileStorage-ის, SmartSchema-სა და ApiClient-ის სახელების სიგრძეს. მათ არსებობას handler-ი
//ამოწმებს (404 ReferencedRecordsNotFound). შეტყობინებები ველს ასახელებს და არა მნიშვნელობას (MediatRLicenseKey
//საიდუმლოა). Version-ს წესი არ სჭირდება: უარყოფითი ვერსია შენახულს არ ემთხვევა (404 ან 409)
public sealed class GlobalSettingsModelValidator : AbstractValidator<StsGlobalSettingsDataModel>
{
    public GlobalSettingsModelValidator()
    {
        RuleFor(x => x.ServiceDescriptionSignature).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.ServiceDescriptionSignature),
            GlobalSettings.ServiceDescriptionSignatureMaxLength);

        RuleFor(x => x.UploadTempExtension).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.UploadTempExtension), GlobalSettings.UploadTempExtensionMaxLength);

        RuleFor(x => x.ProgramArchiveDateMask).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.ProgramArchiveDateMask),
            GlobalSettings.ProgramArchiveDateMaskMaxLength);

        RuleFor(x => x.ProgramArchiveExtension).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.ProgramArchiveExtension),
            GlobalSettings.ProgramArchiveExtensionMaxLength);

        RuleFor(x => x.ParametersFileDateMask).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.ParametersFileDateMask),
            GlobalSettings.ParametersFileDateMaskMaxLength);

        RuleFor(x => x.ParametersFileExtension).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.ParametersFileExtension),
            GlobalSettings.ParametersFileExtensionMaxLength);

        RuleFor(x => x.MediatRLicenseKey).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.MediatRLicenseKey), GlobalSettings.MediatRLicenseKeyMaxLength);

        RuleFor(x => x.FileStorageNameForExchange).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.FileStorageNameForExchange), FileStorage.NameMaxLength);

        RuleFor(x => x.SmartSchemaNameForExchange).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.SmartSchemaNameForExchange), SmartSchema.NameMaxLength);

        RuleFor(x => x.SmartSchemaNameForLocal).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.SmartSchemaNameForLocal), SmartSchema.NameMaxLength);

        RuleFor(x => x.LocalPackageManagerWebApiClientName).OptionalWithMaxLength(
            _ => nameof(StsGlobalSettingsDataModel.LocalPackageManagerWebApiClientName), ApiClient.NameMaxLength);

        //ნაწილი ყოველთვის მოდის, თუნდაც ცარიელი; null ნიშნავს, რომ ის ტანში null-ად ჩაიწერა
        RuleFor(x => x.DatabasesBackupFilesExchange).NotNull()
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired)).WithMessage(_ =>
                SupportToolsServerApiClientErrors
                    .ValueRequired(nameof(StsGlobalSettingsDataModel.DatabasesBackupFilesExchange)).Description)
            .SetValidator(new DatabasesBackupFilesExchangeModelValidator());
    }
}
