using FluentValidation;

namespace SupportToolsServer.Application.Servers.UpdateServer;

// ReSharper disable once UnusedType.Global
public sealed class UpdateServerCommandValidator : AbstractValidator<UpdateServerCommand>
{
    public UpdateServerCommandValidator()
    {
        RuleFor(x => x.Server).SetValidator(new ServerModelValidator());
    }
}
