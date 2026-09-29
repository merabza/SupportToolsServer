using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;

namespace SupportToolsServer.Application.EditorConfigFileTypes;

//ატვირთული .editorconfig შაბლონის წესები. სიგრძეები EditorConfigFileTypeConfiguration-ის HasMaxLength-ს ემთხვევა
public sealed class EditorConfigFileTypeModelValidator : AbstractValidator<StsEditorConfigFileTypeDataModel>
{
    public EditorConfigFileTypeModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsEditorConfigFileTypeDataModel.Name),
            EditorConfigFileType.NameMaxLength);

        //შინაარსი შეიძლება ცარიელი იყოს, მაგრამ არა null
        RuleFor(x => x.Content).NotNull().WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired))
            .WithMessage(x => SupportToolsServerApiClientErrors.ValueRequired(ContentValueName(x)).Description)
            .MaximumLength(EditorConfigFileType.ContentMaxLength)
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueTooLong)).WithMessage(x =>
                SupportToolsServerApiClientErrors
                    .ValueTooLong(ContentValueName(x), EditorConfigFileType.ContentMaxLength).Description);
    }

    private static string ContentValueName(StsEditorConfigFileTypeDataModel editorConfigFileType)
    {
        return string.IsNullOrWhiteSpace(editorConfigFileType.Name)
            ? nameof(StsEditorConfigFileTypeDataModel.Content)
            : $"{editorConfigFileType.Name}.{nameof(StsEditorConfigFileTypeDataModel.Content)}";
    }
}
