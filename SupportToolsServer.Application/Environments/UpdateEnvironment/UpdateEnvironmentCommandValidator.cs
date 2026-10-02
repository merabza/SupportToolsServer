using FluentValidation;

namespace SupportToolsServer.Application.Environments.UpdateEnvironment;

// ReSharper disable once UnusedType.Global
public sealed class UpdateEnvironmentCommandValidator : AbstractValidator<UpdateEnvironmentCommand>
{
    public UpdateEnvironmentCommandValidator()
    {
        RuleFor(x => x.Environment).SetValidator(new EnvironmentModelValidator());
    }
}
