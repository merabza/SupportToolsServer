using FluentValidation;

namespace SupportToolsServer.Application.GitRepos.UpdateGitRepo;

// ReSharper disable once UnusedType.Global
public sealed class UpdateGitRepoCommandValidator : AbstractValidator<UpdateGitRepoCommand>
{
    public UpdateGitRepoCommandValidator()
    {
        RuleFor(x => x.GitRepo).SetValidator(new GitRepoModelValidator());
    }
}
