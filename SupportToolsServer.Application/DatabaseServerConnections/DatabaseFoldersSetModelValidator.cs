using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DatabaseServerConnections;

namespace SupportToolsServer.Application.DatabaseServerConnections;

//ფოლდერების ნაკრების წესები. სიგრძეები DatabaseFoldersSetConfiguration-ის HasMaxLength-ს ემთხვევა. გზები DB სერვერისაა,
//ამიტომ მხოლოდ სიგრძით მოწმდება
public sealed class DatabaseFoldersSetModelValidator : AbstractValidator<StsDatabaseFoldersSetDataModel>
{
    private const string FoldersSetsName = nameof(StsDatabaseServerConnectionDataModel.DatabaseFoldersSets);

    public DatabaseFoldersSetModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => $"{FoldersSetsName}.{nameof(StsDatabaseFoldersSetDataModel.Name)}",
            DatabaseFoldersSet.NameMaxLength);

        RuleFor(x => x.Backup).OptionalWithMaxLength(x => ValueName(x, nameof(StsDatabaseFoldersSetDataModel.Backup)),
            DatabaseFoldersSet.FolderMaxLength);

        RuleFor(x => x.Data).OptionalWithMaxLength(x => ValueName(x, nameof(StsDatabaseFoldersSetDataModel.Data)),
            DatabaseFoldersSet.FolderMaxLength);

        RuleFor(x => x.DataLog).OptionalWithMaxLength(
            x => ValueName(x, nameof(StsDatabaseFoldersSetDataModel.DataLog)), DatabaseFoldersSet.FolderMaxLength);
    }

    private static string ValueName(StsDatabaseFoldersSetDataModel foldersSet, string propertyName)
    {
        return string.IsNullOrWhiteSpace(foldersSet.Name)
            ? $"{FoldersSetsName}.{propertyName}"
            : $"{FoldersSetsName}.{foldersSet.Name}.{propertyName}";
    }
}
