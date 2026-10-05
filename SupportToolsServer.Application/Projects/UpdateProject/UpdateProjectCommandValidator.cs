using FluentValidation;

namespace SupportToolsServer.Application.Projects.UpdateProject;

// ReSharper disable once UnusedType.Global
public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Project).SetValidator(new ProjectModelValidator());
    }
}
