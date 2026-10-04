using FluentValidation;

namespace SupportToolsServer.Application.Runtimes.UpdateRuntime;

// ReSharper disable once UnusedType.Global
public sealed class UpdateRuntimeCommandValidator : AbstractValidator<UpdateRuntimeCommand>
{
    public UpdateRuntimeCommandValidator()
    {
        RuleFor(x => x.Runtime).SetValidator(new RuntimeModelValidator());
    }
}
