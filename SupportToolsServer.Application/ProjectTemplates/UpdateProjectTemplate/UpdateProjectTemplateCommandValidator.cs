using FluentValidation;

namespace SupportToolsServer.Application.ProjectTemplates.UpdateProjectTemplate;

// ReSharper disable once UnusedType.Global
public sealed class UpdateProjectTemplateCommandValidator : AbstractValidator<UpdateProjectTemplateCommand>
{
    public UpdateProjectTemplateCommandValidator()
    {
        RuleFor(x => x.ProjectTemplate).SetValidator(new ProjectTemplateModelValidator());
    }
}
