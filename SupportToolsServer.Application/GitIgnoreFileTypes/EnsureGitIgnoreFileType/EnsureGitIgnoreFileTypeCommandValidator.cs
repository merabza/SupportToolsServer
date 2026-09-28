using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.EnsureGitIgnoreFileType;

// ReSharper disable once UnusedType.Global
public sealed class EnsureGitIgnoreFileTypeCommandValidator : AbstractValidator<EnsureGitIgnoreFileTypeCommand>
{
    public EnsureGitIgnoreFileTypeCommandValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(EnsureGitIgnoreFileTypeCommand.Name),
            GitIgnoreFileType.NameMaxLength);
    }
}
