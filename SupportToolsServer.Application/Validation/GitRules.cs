using System;
using System.Buffers;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using SupportToolsServerApiContracts.Errors;

namespace SupportToolsServer.Application.Validation;

//რეპოზიტორიის ფოლდერისა და მისამართის წესები. ფოლდერის სახელით სერვერი Gits-ში ფოლდერს ქმნის და შლის,
//მისამართით კი git-ს უშვებს, ამიტომ ორივე წინასწარ მოწმდება
internal static partial class GitRules
{
    //კლიენტის GitDataModel.SpaProjectFolderRelativePathName: ასეთი ფოლდერი პროექტის SPA ფოლდერის მიმართ ითვლება
    public const string SpaProjectFolderRelativePathName = "{SpaProjectFolderRelativePath}";

    private static readonly char[] FolderNameSeparators = ['\\', '/'];

    //Windows-ის აკრძალული სიმბოლოები ფიქსირებულად, რომ შედეგი OS-ზე არ იყოს დამოკიდებული. \ და / გამყოფებია,
    //მმართველი სიმბოლოები ცალკე მოწმდება
    private static readonly SearchValues<char> ForbiddenFolderNameChars = SearchValues.Create("<>:\"|?*");

    //ცარიელ მნიშვნელობას RequiredWithMaxLength იჭერს
    public static IRuleBuilderOptions<T, string> ValidGitFolderName<T>(this IRuleBuilder<T, string> ruleBuilder,
        Func<T, string> valueName)
    {
        return ruleBuilder.Must(x => string.IsNullOrWhiteSpace(x) || IsValidFolderName(x))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.InvalidGitFolderName)).WithMessage(x =>
                SupportToolsServerApiClientErrors.InvalidGitFolderName(valueName(x)).Description);
    }

    //ცარიელ მნიშვნელობას RequiredWithMaxLength იჭერს
    public static IRuleBuilderOptions<T, string> ValidGitAddress<T>(this IRuleBuilder<T, string> ruleBuilder,
        Func<T, string> valueName)
    {
        return ruleBuilder.Must(x => string.IsNullOrWhiteSpace(x) || IsValidAddress(x))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.InvalidGitAddress)).WithMessage(x =>
                SupportToolsServerApiClientErrors.InvalidGitAddress(valueName(x)).Description);
    }

    //შეფარდებითი გზა: ერთი ან მეტი სეგმენტი, გამყოფი \ ან /. დასაშვებია SPA-ის წინსართი, რომლის შემდეგაც
    //კლიენტის მსგავსად ერთი გამყოფი გამოიტოვება. ფესვიანი გზა (C:, \\server, /x) ცარიელ სეგმენტს ან ':'-ს შეიცავს
    public static bool IsValidFolderName(string folderName)
    {
        string relativePath = folderName;
        if (relativePath.StartsWith(SpaProjectFolderRelativePathName, StringComparison.Ordinal))
        {
            relativePath = relativePath[SpaProjectFolderRelativePathName.Length..];
            if (relativePath.StartsWith('\\') || relativePath.StartsWith('/'))
            {
                relativePath = relativePath[1..];
            }
        }

        return relativePath.Split(FolderNameSeparators).All(IsValidFolderNameSegment);
    }

    //დასაშვებია მხოლოდ git@host:path, ssh://… და https://…. სამივე ასოთი იწყება, ამიტომ არ გაივლის "-"-ით
    //დაწყებული მისამართი (git-ის ოფცია), file://, ext:: და ლოკალური გზა. ჰოსტი და მომხმარებელი "-"-ით ვერ
    //დაიწყება, რომ ssh-მა ის ოფციად არ აღიქვას
    public static bool IsValidAddress(string address)
    {
        return !address.Any(x => char.IsWhiteSpace(x) || char.IsControl(x)) &&
               (ScpLikeSshAddressRegex().IsMatch(address) || SshUrlAddressRegex().IsMatch(address) ||
                HttpsUrlAddressRegex().IsMatch(address));
    }

    //ბოლო წერტილსა და გამოტოვებას Windows გზის ნორმალიზაციისას აცილებს, ამიტომ "..." და ".. " Gits ფოლდერს
    //დაემთხვეოდა. ეს წესი "."-სა და ".."-საც გამორიცხავს
    private static bool IsValidFolderNameSegment(string segment)
    {
        return segment.Length > 0 && !segment.EndsWith('.') && !segment.EndsWith(' ') &&
               !segment.AsSpan().ContainsAny(ForbiddenFolderNameChars) && !segment.Any(char.IsControl);
    }

    //git@host:path (scp-ის სტილის SSH)
    [GeneratedRegex("^git@[A-Za-z0-9][A-Za-z0-9.-]*:.+$", RegexOptions.None, 1000)]
    private static partial Regex ScpLikeSshAddressRegex();

    //ssh://[user@]host[:port]/path
    [GeneratedRegex("^ssh://(?:[A-Za-z0-9_][A-Za-z0-9._-]*@)?[A-Za-z0-9][A-Za-z0-9.-]*(?::[0-9]+)?/.+$",
        RegexOptions.None, 1000)]
    private static partial Regex SshUrlAddressRegex();

    //https://[userinfo@]host[:port]/path
    [GeneratedRegex("^https://(?:[^@/]+@)?[A-Za-z0-9][A-Za-z0-9.-]*(?::[0-9]+)?/.+$", RegexOptions.None, 1000)]
    private static partial Regex HttpsUrlAddressRegex();
}
