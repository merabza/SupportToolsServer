using FluentValidation;

namespace SupportToolsServer.Application.StoredFiles.UpdateStoredFile;

// ReSharper disable once UnusedType.Global
public sealed class UpdateStoredFileCommandValidator : AbstractValidator<UpdateStoredFileCommand>
{
    public UpdateStoredFileCommandValidator()
    {
        RuleFor(x => x.StoredFile).SetValidator(new StoredFileModelValidator());
    }
}
