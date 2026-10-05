using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.NpmPackages;
using SupportToolsServerCore.Domain.Projects;

namespace SupportToolsServer.Application.Projects;

//პროექტის ველების წესები. სიგრძეები ProjectConfiguration-ის HasMaxLength-ს ემთხვევა, მითითებული ჩანაწერების
//სახელებისა კი EditorConfigFileType-ის, GitRepo-სა და NpmPackage-ის სახელების სიგრძეს. მათ არსებობას handler-ი
//ამოწმებს (404 ReferencedRecordsNotFound). სერვერი კლიენტის enum-ებს არ იცნობს, ამიტომ ProjectType და AllowToolsList
//მხოლოდ სიგრძით მოწმდება. გზები კანონიკური ფორმითაა (README G3) და მხოლოდ სიგრძით მოწმდება. შეტყობინებები ველს
//ასახელებს და არასოდეს მის მნიშვნელობას, რადგან KeyGuidPart საიდუმლოა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class ProjectModelValidator : AbstractValidator<StsProjectDataModel>
{
    public ProjectModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsProjectDataModel.Name), Project.NameMaxLength);

        RuleFor(x => x.ProjectType).RequiredWithMaxLength(
            x => ValueName(x, nameof(StsProjectDataModel.ProjectType)), Project.ProjectTypeMaxLength);

        OptionalText(x => x.ProjectGroupName, nameof(StsProjectDataModel.ProjectGroupName),
            Project.ProjectGroupNameMaxLength);
        OptionalText(x => x.ProjectDescription, nameof(StsProjectDataModel.ProjectDescription),
            Project.ProjectDescriptionMaxLength);
        OptionalText(x => x.EditorConfigPatternName, nameof(StsProjectDataModel.EditorConfigPatternName),
            EditorConfigFileType.NameMaxLength);

        OptionalText(x => x.MainProjectName, nameof(StsProjectDataModel.MainProjectName), Project.CodeNameMaxLength);
        OptionalText(x => x.ApiContractsProjectName, nameof(StsProjectDataModel.ApiContractsProjectName),
            Project.CodeNameMaxLength);
        OptionalText(x => x.SpaProjectName, nameof(StsProjectDataModel.SpaProjectName), Project.CodeNameMaxLength);
        OptionalText(x => x.DbContextName, nameof(StsProjectDataModel.DbContextName), Project.CodeNameMaxLength);
        OptionalText(x => x.ProjectShortPrefix, nameof(StsProjectDataModel.ProjectShortPrefix),
            Project.CodeNameMaxLength);
        OptionalText(x => x.ScaffoldSeederProjectName, nameof(StsProjectDataModel.ScaffoldSeederProjectName),
            Project.CodeNameMaxLength);
        OptionalText(x => x.DbContextProjectName, nameof(StsProjectDataModel.DbContextProjectName),
            Project.CodeNameMaxLength);
        OptionalText(x => x.NewDataSeedingClassLibProjectName,
            nameof(StsProjectDataModel.NewDataSeedingClassLibProjectName), Project.CodeNameMaxLength);

        OptionalText(x => x.ProgramArchiveDateMask, nameof(StsProjectDataModel.ProgramArchiveDateMask),
            Project.MaskMaxLength);
        OptionalText(x => x.ProgramArchiveExtension, nameof(StsProjectDataModel.ProgramArchiveExtension),
            Project.MaskMaxLength);
        OptionalText(x => x.ParametersFileDateMask, nameof(StsProjectDataModel.ParametersFileDateMask),
            Project.MaskMaxLength);
        OptionalText(x => x.ParametersFileExtension, nameof(StsProjectDataModel.ParametersFileExtension),
            Project.MaskMaxLength);

        OptionalText(x => x.ProjectFolderName, nameof(StsProjectDataModel.ProjectFolderName), Project.PathMaxLength);
        OptionalText(x => x.SolutionFileName, nameof(StsProjectDataModel.SolutionFileName), Project.PathMaxLength);
        OptionalText(x => x.ProjectSecurityFolderPath, nameof(StsProjectDataModel.ProjectSecurityFolderPath),
            Project.PathMaxLength);
        OptionalText(x => x.MigrationStartupProjectFilePath,
            nameof(StsProjectDataModel.MigrationStartupProjectFilePath), Project.PathMaxLength);
        OptionalText(x => x.MigrationProjectFilePath, nameof(StsProjectDataModel.MigrationProjectFilePath),
            Project.PathMaxLength);
        OptionalText(x => x.DataSeederRulesByTableStartupProjectFilePath,
            nameof(StsProjectDataModel.DataSeederRulesByTableStartupProjectFilePath), Project.PathMaxLength);
        OptionalText(x => x.OldDataConvertorForDataSeeder, nameof(StsProjectDataModel.OldDataConvertorForDataSeeder),
            Project.PathMaxLength);
        OptionalText(x => x.SeedProjectFilePath, nameof(StsProjectDataModel.SeedProjectFilePath),
            Project.PathMaxLength);
        OptionalText(x => x.SeedProjectParametersFilePath, nameof(StsProjectDataModel.SeedProjectParametersFilePath),
            Project.PathMaxLength);
        OptionalText(x => x.ExcludesRulesParametersFilePath,
            nameof(StsProjectDataModel.ExcludesRulesParametersFilePath), Project.PathMaxLength);
        OptionalText(x => x.AppSetEnKeysJsonFileName, nameof(StsProjectDataModel.AppSetEnKeysJsonFileName),
            Project.PathMaxLength);
        OptionalText(x => x.MigrationSqlFilesFolder, nameof(StsProjectDataModel.MigrationSqlFilesFolder),
            Project.PathMaxLength);
        OptionalText(x => x.PrepareProdCopyDatabaseProjectFilePath,
            nameof(StsProjectDataModel.PrepareProdCopyDatabaseProjectFilePath), Project.PathMaxLength);
        OptionalText(x => x.PrepareProdCopyDatabaseProjectParametersFilePath,
            nameof(StsProjectDataModel.PrepareProdCopyDatabaseProjectParametersFilePath), Project.PathMaxLength);
        OptionalText(x => x.PairedDbObjectsResultFileName, nameof(StsProjectDataModel.PairedDbObjectsResultFileName),
            Project.PathMaxLength);

        OptionalText(x => x.KeyGuidPart, nameof(StsProjectDataModel.KeyGuidPart), Project.KeyGuidPartMaxLength);

        //null ნიშნავს, რომ პროექტს ეს პარამეტრები არ აქვს
        RuleFor(x => x.DevDatabaseParameters!).SetValidator(
            new DatabaseParametersModelValidator(nameof(StsProjectDataModel.DevDatabaseParameters)));
        RuleFor(x => x.ProdCopyDatabaseParameters!).SetValidator(
            new DatabaseParametersModelValidator(nameof(StsProjectDataModel.ProdCopyDatabaseParameters)));

        NameList(x => x.GitProjectNames, nameof(StsProjectDataModel.GitProjectNames), GitRepo.NameMaxLength);
        NameList(x => x.ScaffoldSeederGitProjectNames, nameof(StsProjectDataModel.ScaffoldSeederGitProjectNames),
            GitRepo.NameMaxLength);
        NameList(x => x.FrontNpmPackageNames, nameof(StsProjectDataModel.FrontNpmPackageNames),
            NpmPackage.NameMaxLength);
        NameList(x => x.RedundantFileNames, nameof(StsProjectDataModel.RedundantFileNames),
            ProjectRedundantFile.FileNameMaxLength);
        NameList(x => x.AllowToolsList, nameof(StsProjectDataModel.AllowToolsList),
            ProjectAllowedTool.ToolNameMaxLength);

        //key-ები კლიენტის dictionary-ის key-ებია, ამიტომ პროექტში ერთხელ უნდა შეგვხვდეს
        RecordList(x => x.Endpoints, nameof(StsProjectDataModel.Endpoints), new ProjectEndpointModelValidator(),
            x => x.Name);
        RecordList(x => x.RouteClasses, nameof(StsProjectDataModel.RouteClasses),
            new ProjectRouteClassModelValidator(), x => x.Name);
    }

    private static string ValueName(StsProjectDataModel project, string propertyName)
    {
        return string.IsNullOrWhiteSpace(project.Name) ? propertyName : $"{project.Name}.{propertyName}";
    }

    private void OptionalText(Expression<Func<StsProjectDataModel, string?>> property, string propertyName,
        int maxLength)
    {
        RuleFor(property).OptionalWithMaxLength(x => ValueName(x, propertyName), maxLength);
    }

    //სახელების სია (სიმრავლე): ცარიელი სია დასაშვებია, მაგრამ არა null (კლიენტის null სია ცარიელად გადაიცემა). ყოველი
    //სახელი შევსებულია, სვეტზე გრძელი არ არის და სიაში ერთხელ გვხვდება, რეგისტრის გარეშე
    private void NameList(Expression<Func<StsProjectDataModel, IEnumerable<string>>> list, string listName,
        int maxLength)
    {
        ListRequired(list, listName);

        RuleForEach(list).RequiredWithMaxLength(x => ValueName(x, listName), maxLength);

        RuleFor(list).Must(x => x is null || UniqueValues.AreUnique(x))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(x =>
                SupportToolsServerApiClientErrors.ValuesNotUnique(ValueName(x, listName)).Description);
    }

    //ჩანაწერების სია (კლიენტის dictionary): ჩანაწერი null ვერ იქნება, key კი სიაში ერთხელ გვხვდება, რეგისტრის გარეშე
    private void RecordList<TRecord>(Expression<Func<StsProjectDataModel, IEnumerable<TRecord>>> list,
        string listName, IValidator<TRecord> recordValidator, Func<TRecord, string> keyOf) where TRecord : class
    {
        ListRequired(list, listName);

        RuleForEach(list).NotNull().WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired))
            .WithMessage(x => SupportToolsServerApiClientErrors.ValueRequired(ValueName(x, listName)).Description)
            .SetValidator(recordValidator);

        RuleFor(list).Must(x => x is null || UniqueValues.AreUnique(x.Select(y => y is null ? null : keyOf(y))))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(x =>
                SupportToolsServerApiClientErrors.ValuesNotUnique(ValueName(x, $"{listName}.Name")).Description);
    }

    private void ListRequired<TItem>(Expression<Func<StsProjectDataModel, IEnumerable<TItem>>> list,
        string listName)
    {
        RuleFor(list).NotNull().WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired))
            .WithMessage(x => SupportToolsServerApiClientErrors.ValueRequired(ValueName(x, listName)).Description);
    }
}
