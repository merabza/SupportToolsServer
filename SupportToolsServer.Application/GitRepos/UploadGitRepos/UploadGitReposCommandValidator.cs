using System.Linq;
using FluentValidation;
using SupportToolsServer.Application.GitIgnoreFileTypes;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;

namespace SupportToolsServer.Application.GitRepos.UploadGitRepos;

// ReSharper disable once UnusedType.Global
public sealed class UploadGitReposCommandValidator : AbstractValidator<UploadGitReposCommand>
{
    public UploadGitReposCommandValidator()
    {
        RuleFor(x => x.Gits).NotNull().WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired))
            .WithMessage(SupportToolsServerApiClientErrors.ValueRequired(nameof(UploadGitReposCommand.Gits))
                .Description);
        RuleFor(x => x.GitIgnoreFiles).NotNull()
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired)).WithMessage(
                SupportToolsServerApiClientErrors.ValueRequired(nameof(UploadGitReposCommand.GitIgnoreFiles))
                    .Description);

        RuleForEach(x => x.Gits).SetValidator(new GitRepoModelValidator());
        RuleForEach(x => x.GitIgnoreFiles).SetValidator(new GitIgnoreFileTypeModelValidator());

        //სახელები და მისამართები ბაზაში უნიკალურია, ამიტომ ატვირთულ სიაშიც არ უნდა მეორდებოდეს
        RuleFor(x => x.Gits).Must(x => x is null || UniqueValues.AreUnique(x.Select(y => y.GitProjectName)))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(
                SupportToolsServerApiClientErrors.ValuesNotUnique(nameof(StsGitDataModel.GitProjectName))
                    .Description);
        RuleFor(x => x.Gits).Must(x => x is null || UniqueValues.AreUnique(x.Select(y => y.GitProjectAddress)))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(
                SupportToolsServerApiClientErrors.ValuesNotUnique(nameof(StsGitDataModel.GitProjectAddress))
                    .Description);
        RuleFor(x => x.GitIgnoreFiles).Must(x => x is null || UniqueValues.AreUnique(x.Select(y => y.Name)))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(
                SupportToolsServerApiClientErrors.ValuesNotUnique(nameof(StsGitIgnoreFileTypeDataModel.Name))
                    .Description);
    }
}
