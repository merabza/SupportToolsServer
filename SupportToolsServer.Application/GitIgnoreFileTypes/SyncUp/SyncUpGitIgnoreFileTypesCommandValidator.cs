using System.Linq;
using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;

// ReSharper disable once UnusedType.Global
public sealed class SyncUpGitIgnoreFileTypesCommandValidator : AbstractValidator<SyncUpGitIgnoreFileTypesCommand>
{
    public SyncUpGitIgnoreFileTypesCommandValidator()
    {
        RuleFor(x => x.UploadGitIgnoreFileTypes).NotNull()
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired)).WithMessage(
                SupportToolsServerApiClientErrors
                    .ValueRequired(nameof(SyncUpGitIgnoreFileTypesCommand.UploadGitIgnoreFileTypes)).Description);

        RuleForEach(x => x.UploadGitIgnoreFileTypes).SetValidator(new GitIgnoreFileTypeModelValidator());

        //ჩანაწერები სერვერისას სახელით ემთხვევა, ამიტომ სახელი სიაში ერთხელ უნდა შეგვხვდეს
        RuleFor(x => x.UploadGitIgnoreFileTypes)
            .Must(x => x is null || UniqueValues.AreUnique(x.Select(y => y.Name)))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(
                SupportToolsServerApiClientErrors.ValuesNotUnique(nameof(StsGitIgnoreFileTypeDataModel.Name))
                    .Description);
    }
}
