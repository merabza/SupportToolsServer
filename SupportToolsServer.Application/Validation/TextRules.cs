using System;
using FluentValidation;
using SupportToolsServerApiContracts.Errors;

namespace SupportToolsServer.Application.Validation;

internal static class TextRules
{
    //ტექსტური ველი შევსებული უნდა იყოს და ბაზის სვეტის სიგრძეს არ უნდა აღემატებოდეს.
    //valueName შეტყობინებაში ველს ასახელებს, საჭიროებისას ჩანაწერის სახელთან ერთად
    public static IRuleBuilderOptions<T, string> RequiredWithMaxLength<T>(this IRuleBuilder<T, string> ruleBuilder,
        Func<T, string> valueName, int maxLength)
    {
        return ruleBuilder.NotEmpty().WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired))
            .WithMessage(x => SupportToolsServerApiClientErrors.ValueRequired(valueName(x)).Description)
            .MaximumLength(maxLength).WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueTooLong))
            .WithMessage(x => SupportToolsServerApiClientErrors.ValueTooLong(valueName(x), maxLength).Description);
    }

    //არასავალდებულო ტექსტური ველი: null და ცარიელი მნიშვნელობა დასაშვებია, სხვა მნიშვნელობა კი ბაზის სვეტის სიგრძეს
    //არ უნდა აღემატებოდეს
    public static IRuleBuilderOptions<T, string?> OptionalWithMaxLength<T>(this IRuleBuilder<T, string?> ruleBuilder,
        Func<T, string> valueName, int maxLength)
    {
        return ruleBuilder.MaximumLength(maxLength)
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueTooLong)).WithMessage(x =>
                SupportToolsServerApiClientErrors.ValueTooLong(valueName(x), maxLength).Description);
    }
}
