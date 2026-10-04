using FluentValidation;

namespace SupportToolsServer.Application.ApiClients.UpdateApiClient;

// ReSharper disable once UnusedType.Global
public sealed class UpdateApiClientCommandValidator : AbstractValidator<UpdateApiClientCommand>
{
    public UpdateApiClientCommandValidator()
    {
        RuleFor(x => x.ApiClient).SetValidator(new ApiClientModelValidator());
    }
}
