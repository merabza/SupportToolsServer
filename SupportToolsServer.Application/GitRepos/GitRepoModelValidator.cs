using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;

namespace SupportToolsServer.Application.GitRepos;

//რეპოზიტორიის ველების წესები UpdateGitRepo-სა და UploadGitRepos-ისთვის.
//სიგრძეები GitRepoConfiguration-ის HasMaxLength-ს ემთხვევა, ფოლდერისა და მისამართის ფორმა GitRules-შია
public sealed class GitRepoModelValidator : AbstractValidator<StsGitDataModel>
{
    public GitRepoModelValidator()
    {
        RuleFor(x => x.GitProjectName).RequiredWithMaxLength(_ => nameof(StsGitDataModel.GitProjectName),
            GitRepo.NameMaxLength);

        RuleFor(x => x.GitProjectAddress)
            .RequiredWithMaxLength(x => ValueName(x, nameof(StsGitDataModel.GitProjectAddress)),
                GitRepo.AddressMaxLength).ValidGitAddress(x => ValueName(x, nameof(StsGitDataModel.GitProjectAddress)));

        RuleFor(x => x.GitProjectFolderName)
            .RequiredWithMaxLength(x => ValueName(x, nameof(StsGitDataModel.GitProjectFolderName)),
                GitRepo.FolderNameMaxLength)
            .ValidGitFolderName(x => ValueName(x, nameof(StsGitDataModel.GitProjectFolderName)));

        RuleFor(x => x.GitIgnorePatternName).RequiredWithMaxLength(
            x => ValueName(x, nameof(StsGitDataModel.GitIgnorePatternName)), GitIgnoreFileType.NameMaxLength);
    }

    //ატვირთვისას შეტყობინებიდან უნდა ჩანდეს, რომელ რეპოზიტორიას ეხება
    private static string ValueName(StsGitDataModel gitRepo, string propertyName)
    {
        return string.IsNullOrWhiteSpace(gitRepo.GitProjectName)
            ? propertyName
            : $"{gitRepo.GitProjectName}.{propertyName}";
    }
}
