using FluentValidation;

namespace SupportToolsServer.Application.DatabaseServerConnections.UpdateDatabaseServerConnection;

// ReSharper disable once UnusedType.Global
public sealed class UpdateDatabaseServerConnectionCommandValidator :
    AbstractValidator<UpdateDatabaseServerConnectionCommand>
{
    public UpdateDatabaseServerConnectionCommandValidator()
    {
        RuleFor(x => x.DatabaseServerConnection).SetValidator(new DatabaseServerConnectionModelValidator());
    }
}
