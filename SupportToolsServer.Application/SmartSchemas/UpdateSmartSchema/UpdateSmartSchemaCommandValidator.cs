using FluentValidation;

namespace SupportToolsServer.Application.SmartSchemas.UpdateSmartSchema;

// ReSharper disable once UnusedType.Global
public sealed class UpdateSmartSchemaCommandValidator : AbstractValidator<UpdateSmartSchemaCommand>
{
    public UpdateSmartSchemaCommandValidator()
    {
        RuleFor(x => x.SmartSchema).SetValidator(new SmartSchemaModelValidator());
    }
}
