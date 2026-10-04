using System.Linq;
using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.ApiClients;
using SupportToolsServerCore.Domain.DatabaseServerConnections;

namespace SupportToolsServer.Application.DatabaseServerConnections;

//ბაზის სერვერთან კავშირის ველების წესები. სიგრძეები DatabaseServerConnectionConfiguration-ის HasMaxLength-ს
//ემთხვევა. სერვერი კლიენტის EDatabaseProvider-ს არ იცნობს, ამიტომ პროვაიდერი მხოლოდ სიგრძით მოწმდება. ვებაგენტის
//არსებობას handler-ი ამოწმებს (404 ReferencedRecordsNotFound). შეტყობინებები ველს ასახელებს და არასოდეს მის
//მნიშვნელობას, რადგან მომხმარებელი და პაროლი საიდუმლოა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class DatabaseServerConnectionModelValidator : AbstractValidator<StsDatabaseServerConnectionDataModel>
{
    public DatabaseServerConnectionModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsDatabaseServerConnectionDataModel.Name),
            DatabaseServerConnection.NameMaxLength);

        RuleFor(x => x.DatabaseServerProvider).RequiredWithMaxLength(
            x => ValueName(x, nameof(StsDatabaseServerConnectionDataModel.DatabaseServerProvider)),
            DatabaseServerConnection.DatabaseServerProviderMaxLength);

        RuleFor(x => x.DbWebAgentName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsDatabaseServerConnectionDataModel.DbWebAgentName)), ApiClient.NameMaxLength);

        RuleFor(x => x.RemoteDbConnectionName).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsDatabaseServerConnectionDataModel.RemoteDbConnectionName)),
            DatabaseServerConnection.RemoteDbConnectionNameMaxLength);

        RuleFor(x => x.ServerAddress).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsDatabaseServerConnectionDataModel.ServerAddress)),
            DatabaseServerConnection.ServerAddressMaxLength);

        RuleFor(x => x.ServerUser).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsDatabaseServerConnectionDataModel.ServerUser)),
            DatabaseServerConnection.ServerUserMaxLength);

        RuleFor(x => x.ServerPass).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsDatabaseServerConnectionDataModel.ServerPass)),
            DatabaseServerConnection.ServerPassMaxLength);

        //ცარიელი სია დასაშვებია, მაგრამ არა null (კლიენტის null dictionary ცარიელ სიად გადაიცემა)
        RuleFor(x => x.DatabaseFoldersSets).NotNull()
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired)).WithMessage(x =>
                SupportToolsServerApiClientErrors.ValueRequired(ValueName(x,
                    nameof(StsDatabaseServerConnectionDataModel.DatabaseFoldersSets))).Description);

        RuleForEach(x => x.DatabaseFoldersSets).SetValidator(new DatabaseFoldersSetModelValidator());

        //ნაკრების სახელი კლიენტის dictionary-ის key-ა, ამიტომ კავშირში ერთხელ უნდა შეგვხვდეს
        RuleFor(x => x.DatabaseFoldersSets).Must(x => x is null || UniqueValues.AreUnique(x.Select(y => y.Name)))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(x =>
                SupportToolsServerApiClientErrors.ValuesNotUnique(ValueName(x,
                    $"{nameof(StsDatabaseServerConnectionDataModel.DatabaseFoldersSets)}.{nameof(StsDatabaseFoldersSetDataModel.Name)}"))
                    .Description);
    }

    private static string ValueName(StsDatabaseServerConnectionDataModel connection, string propertyName)
    {
        return string.IsNullOrWhiteSpace(connection.Name) ? propertyName : $"{connection.Name}.{propertyName}";
    }
}
