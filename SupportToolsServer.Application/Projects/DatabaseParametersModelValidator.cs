using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Projects;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.Projects;

//ბაზის პარამეტრების წესები. სიგრძეები DatabaseParametersConfiguration-ის HasMaxLength-ს ემთხვევა, მითითებული
//ჩანაწერების სახელებისა კი DatabaseServerConnection-ის, SmartSchema-სა და FileStorage-ის სახელების სიგრძეს. მათ
//არსებობას handler-ი ამოწმებს (404 ReferencedRecordsNotFound). სერვერი კლიენტის enum-ებს არ იცნობს, ამიტომ
//DatabaseRecoveryModel და BackupType მხოლოდ სიგრძით მოწმდება. შეტყობინებები ველს ნაწილის სახელით ასახელებს
//(DevDatabaseParameters.DatabaseName). ვალიდატორი public-ია და პარამეტრის გარეშე კონსტრუქტორიც აქვს, რადგან
//AddFluentValidation ყველა public ვალიდატორს DI-ში არეგისტრირებს; ნაწილის სახელს მფლობელის ვალიდატორი შიდა
//კონსტრუქტორით გადასცემს
public sealed class DatabaseParametersModelValidator : AbstractValidator<StsDatabaseParametersDataModel>
{
    public DatabaseParametersModelValidator() : this("DatabaseParameters")
    {
    }

    internal DatabaseParametersModelValidator(string partName)
    {
        RuleFor(x => x.DbConnectionName).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.DbConnectionName)),
            DatabaseServerConnection.NameMaxLength);

        RuleFor(x => x.DatabaseRecoveryModel).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.DatabaseRecoveryModel)),
            DatabaseParameters.DatabaseRecoveryModelMaxLength);

        RuleFor(x => x.DbServerFoldersSetName).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.DbServerFoldersSetName)),
            DatabaseParameters.DbServerFoldersSetNameMaxLength);

        RuleFor(x => x.DatabaseName).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.DatabaseName)),
            DatabaseParameters.DatabaseNameMaxLength);

        RuleFor(x => x.SmartSchemaName).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.SmartSchemaName)),
            SmartSchema.NameMaxLength);

        RuleFor(x => x.FileStorageName).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.FileStorageName)),
            FileStorage.NameMaxLength);

        RuleFor(x => x.BackupNamePrefix).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.BackupNamePrefix)),
            DatabaseParameters.BackupNamePrefixMaxLength);

        RuleFor(x => x.DateMask).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.DateMask)),
            DatabaseParameters.DateMaskMaxLength);

        RuleFor(x => x.BackupFileExtension).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.BackupFileExtension)),
            DatabaseParameters.BackupFileExtensionMaxLength);

        RuleFor(x => x.BackupNameMiddlePart).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.BackupNameMiddlePart)),
            DatabaseParameters.BackupNameMiddlePartMaxLength);

        RuleFor(x => x.BackupType).OptionalWithMaxLength(
            _ => FieldName(partName, nameof(StsDatabaseParametersDataModel.BackupType)),
            DatabaseParameters.BackupTypeMaxLength);
    }

    private static string FieldName(string partName, string propertyName)
    {
        return $"{partName}.{propertyName}";
    }
}
