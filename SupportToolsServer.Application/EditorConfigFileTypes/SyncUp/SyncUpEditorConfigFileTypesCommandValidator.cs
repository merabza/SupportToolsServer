using System.Linq;
using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServer.Application.EditorConfigFileTypes.SyncUp;

// ReSharper disable once UnusedType.Global
public sealed class SyncUpEditorConfigFileTypesCommandValidator : AbstractValidator<SyncUpEditorConfigFileTypesCommand>
{
    public SyncUpEditorConfigFileTypesCommandValidator()
    {
        RuleFor(x => x.UploadEditorConfigFileTypes).NotNull()
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired)).WithMessage(
                SupportToolsServerApiClientErrors
                    .ValueRequired(nameof(SyncUpEditorConfigFileTypesCommand.UploadEditorConfigFileTypes))
                    .Description);

        RuleForEach(x => x.UploadEditorConfigFileTypes).SetValidator(new EditorConfigFileTypeModelValidator());

        //ჩანაწერები სერვერისას სახელით ემთხვევა, ამიტომ სახელი სიაში ერთხელ უნდა შეგვხვდეს
        RuleFor(x => x.UploadEditorConfigFileTypes)
            .Must(x => x is null || UniqueValues.AreUnique(x.Select(y => y.Name)))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(
                SupportToolsServerApiClientErrors.ValuesNotUnique(nameof(StsEditorConfigFileTypeDataModel.Name))
                    .Description);
    }
}
