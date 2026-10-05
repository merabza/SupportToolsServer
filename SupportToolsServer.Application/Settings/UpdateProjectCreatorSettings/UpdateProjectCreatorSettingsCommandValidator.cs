using FluentValidation;

namespace SupportToolsServer.Application.Settings.UpdateProjectCreatorSettings;

// ReSharper disable once UnusedType.Global
public sealed class
    UpdateProjectCreatorSettingsCommandValidator : AbstractValidator<UpdateProjectCreatorSettingsCommand>
{
    public UpdateProjectCreatorSettingsCommandValidator()
    {
        RuleFor(x => x.ProjectCreatorSettings).SetValidator(new ProjectCreatorSettingsModelValidator());
    }
}
