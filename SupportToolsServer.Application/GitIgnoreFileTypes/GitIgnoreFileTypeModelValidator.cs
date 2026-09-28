using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;

namespace SupportToolsServer.Application.GitIgnoreFileTypes;

//ატვირთული gitignore ფაილის ტიპის წესები. სიგრძეები GitIgnoreFileTypeConfiguration-ის HasMaxLength-ს ემთხვევა
public sealed class GitIgnoreFileTypeModelValidator : AbstractValidator<StsGitIgnoreFileTypeDataModel>
{
    public GitIgnoreFileTypeModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsGitIgnoreFileTypeDataModel.Name),
            GitIgnoreFileType.NameMaxLength);

        //შინაარსი შეიძლება ცარიელი იყოს, მაგრამ არა null
        RuleFor(x => x.Content).NotNull().WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired))
            .WithMessage(x => SupportToolsServerApiClientErrors.ValueRequired(ContentValueName(x)).Description)
            .MaximumLength(GitIgnoreFileType.ContentMaxLength)
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueTooLong)).WithMessage(x =>
                SupportToolsServerApiClientErrors.ValueTooLong(ContentValueName(x), GitIgnoreFileType.ContentMaxLength)
                    .Description);
    }

    private static string ContentValueName(StsGitIgnoreFileTypeDataModel gitIgnoreFileType)
    {
        return string.IsNullOrWhiteSpace(gitIgnoreFileType.Name)
            ? nameof(StsGitIgnoreFileTypeDataModel.Content)
            : $"{gitIgnoreFileType.Name}.{nameof(StsGitIgnoreFileTypeDataModel.Content)}";
    }
}
