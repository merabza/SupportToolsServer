using System;
using System.Buffers;
using System.Linq;
using FluentValidation;
using SupportToolsServerApiContracts.Errors;

namespace SupportToolsServer.Application.Validation;

//კანონიკური გზის წესი (README G3): Windows-ის აბსოლუტური გზა, როგორც მთავარ კომპიუტერზეა (X:\...). საიდუმლო ფაილის
//გზა ჩანაწერის გასაღებია, ამიტომ ერთ ფაილს ერთი წერილობა უნდა ჰქონდეს: გამყოფი მხოლოდ \-ია
internal static class PathRules
{
    private const char Separator = '\\';

    //Windows-ის აკრძალული სიმბოლოები ფიქსირებულად, როგორც GitRules-ში, რომ შედეგი OS-ზე არ იყოს დამოკიდებული. \
    //გამყოფია, / კი აკრძალულია, რომ ერთი ფაილი ორი წერილობით არ შეინახოს. მმართველი სიმბოლოები ცალკე მოწმდება
    private static readonly SearchValues<char> ForbiddenFileNameChars = SearchValues.Create("<>:\"/|?*");

    //ცარიელ მნიშვნელობას RequiredWithMaxLength იჭერს
    public static IRuleBuilderOptions<T, string> ValidAbsoluteFilePath<T>(this IRuleBuilder<T, string> ruleBuilder,
        Func<T, string> valueName)
    {
        return ruleBuilder.Must(x => string.IsNullOrWhiteSpace(x) || IsValidAbsoluteFilePath(x))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.InvalidFilePath)).WithMessage(x =>
                SupportToolsServerApiClientErrors.InvalidFilePath(valueName(x)).Description);
    }

    //დისკის ასო, ":\" და ერთი ან მეტი სეგმენტი \-ით. ამიტომ გამოირიცხება UNC (\\server), \\?\, შეფარდებითი გზა,
    //მხოლოდ დისკი (D:\) და ცარიელი სეგმენტი (\\ ან ბოლო \)
    public static bool IsValidAbsoluteFilePath(string path)
    {
        return path.Length > 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] == Separator &&
               path[3..].Split(Separator).All(IsValidSegment);
    }

    //ბოლო წერტილსა და გამოტოვებას Windows გზის ნორმალიზაციისას აცილებს, ამიტომ "a." და "a" ერთი ფაილი იქნებოდა. ეს
    //წესი "."-სა და ".."-საც გამორიცხავს
    private static bool IsValidSegment(string segment)
    {
        return segment.Length > 0 && !segment.EndsWith('.') && !segment.EndsWith(' ') &&
               !segment.AsSpan().ContainsAny(ForbiddenFileNameChars) && !segment.Any(char.IsControl);
    }
}
