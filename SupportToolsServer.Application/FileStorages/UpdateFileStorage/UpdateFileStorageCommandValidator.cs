using FluentValidation;

namespace SupportToolsServer.Application.FileStorages.UpdateFileStorage;

// ReSharper disable once UnusedType.Global
public sealed class UpdateFileStorageCommandValidator : AbstractValidator<UpdateFileStorageCommand>
{
    public UpdateFileStorageCommandValidator()
    {
        RuleFor(x => x.FileStorage).SetValidator(new FileStorageModelValidator());
    }
}
