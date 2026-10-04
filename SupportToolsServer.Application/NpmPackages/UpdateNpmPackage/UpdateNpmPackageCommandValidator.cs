using FluentValidation;

namespace SupportToolsServer.Application.NpmPackages.UpdateNpmPackage;

// ReSharper disable once UnusedType.Global
public sealed class UpdateNpmPackageCommandValidator : AbstractValidator<UpdateNpmPackageCommand>
{
    public UpdateNpmPackageCommandValidator()
    {
        RuleFor(x => x.NpmPackage).SetValidator(new NpmPackageModelValidator());
    }
}
