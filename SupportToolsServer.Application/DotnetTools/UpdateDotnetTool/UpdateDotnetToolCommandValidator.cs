using FluentValidation;

namespace SupportToolsServer.Application.DotnetTools.UpdateDotnetTool;

// ReSharper disable once UnusedType.Global
public sealed class UpdateDotnetToolCommandValidator : AbstractValidator<UpdateDotnetToolCommand>
{
    public UpdateDotnetToolCommandValidator()
    {
        RuleFor(x => x.DotnetTool).SetValidator(new DotnetToolModelValidator());
    }
}
