using FluentValidation;

namespace SupportToolsServer.Application.ReactAppTemplates.UpdateReactAppTemplate;

// ReSharper disable once UnusedType.Global
public sealed class UpdateReactAppTemplateCommandValidator : AbstractValidator<UpdateReactAppTemplateCommand>
{
    public UpdateReactAppTemplateCommandValidator()
    {
        RuleFor(x => x.ReactAppTemplate).SetValidator(new ReactAppTemplateModelValidator());
    }
}
