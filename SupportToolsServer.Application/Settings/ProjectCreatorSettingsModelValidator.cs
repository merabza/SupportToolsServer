using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;
using SupportToolsServerCore.Domain.DeploymentEnvironments;
using SupportToolsServerCore.Domain.FileStorages;
using SupportToolsServerCore.Domain.Servers;
using SupportToolsServerCore.Domain.Settings;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.Settings;

//პროექტის შემქმნელის პარამეტრების წესები. სიგრძეები ProjectCreatorSettingsConfiguration-ის HasMaxLength-ს ემთხვევა,
//მითითებული ჩანაწერების სახელებისა კი Server-ის, Environment-ის, DatabaseServerConnection-ის, FileStorage-ისა და
//SmartSchema-ს სახელების სიგრძეს. მათ არსებობას handler-ი ამოწმებს (404 ReferencedRecordsNotFound). გზები კანონიკურია
//და მხოლოდ სიგრძით მოწმდება. IndentSize-ს წესი არ აქვს, კლიენტშიც არ აქვს.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია შენახულს არ ემთხვევა (404 ან 409)
public sealed class ProjectCreatorSettingsModelValidator : AbstractValidator<StsProjectCreatorSettingsDataModel>
{
    public ProjectCreatorSettingsModelValidator()
    {
        RuleFor(x => x.FakeHostProjectName).OptionalWithMaxLength(
            _ => nameof(StsProjectCreatorSettingsDataModel.FakeHostProjectName),
            ProjectCreatorSettings.FakeHostProjectNameMaxLength);

        RuleFor(x => x.ProjectsFolderPathReal).OptionalWithMaxLength(
            _ => nameof(StsProjectCreatorSettingsDataModel.ProjectsFolderPathReal),
            ProjectCreatorSettings.ProjectsFolderPathRealMaxLength);

        RuleFor(x => x.SecretsFolderPathReal).OptionalWithMaxLength(
            _ => nameof(StsProjectCreatorSettingsDataModel.SecretsFolderPathReal),
            ProjectCreatorSettings.SecretsFolderPathRealMaxLength);

        RuleFor(x => x.ProductionServerName).OptionalWithMaxLength(
            _ => nameof(StsProjectCreatorSettingsDataModel.ProductionServerName), Server.NameMaxLength);

        RuleFor(x => x.ProductionEnvironmentName).OptionalWithMaxLength(
            _ => nameof(StsProjectCreatorSettingsDataModel.ProductionEnvironmentName),
            DeploymentEnvironment.NameMaxLength);

        RuleFor(x => x.DeveloperDbConnectionName).OptionalWithMaxLength(
            _ => nameof(StsProjectCreatorSettingsDataModel.DeveloperDbConnectionName),
            DatabaseServerConnection.NameMaxLength);

        RuleFor(x => x.DatabaseExchangeFileStorageName).OptionalWithMaxLength(
            _ => nameof(StsProjectCreatorSettingsDataModel.DatabaseExchangeFileStorageName),
            FileStorage.NameMaxLength);

        RuleFor(x => x.UseSmartSchema).OptionalWithMaxLength(
            _ => nameof(StsProjectCreatorSettingsDataModel.UseSmartSchema), SmartSchema.NameMaxLength);
    }
}
