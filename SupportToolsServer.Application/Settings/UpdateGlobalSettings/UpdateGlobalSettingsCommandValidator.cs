using FluentValidation;

namespace SupportToolsServer.Application.Settings.UpdateGlobalSettings;

// ReSharper disable once UnusedType.Global
public sealed class UpdateGlobalSettingsCommandValidator : AbstractValidator<UpdateGlobalSettingsCommand>
{
    public UpdateGlobalSettingsCommandValidator()
    {
        RuleFor(x => x.GlobalSettings).SetValidator(new GlobalSettingsModelValidator());
    }
}
