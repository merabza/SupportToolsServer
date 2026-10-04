using System.Linq;
using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.SmartSchemas;

//ჭკვიანი სქემის ველების წესები. სიგრძეები SmartSchemaConfiguration-ის HasMaxLength-ს ემთხვევა.
//Version-ს წესი არ სჭირდება: უარყოფითი ვერსია არც ერთ ჩანაწერს არ ემთხვევა (404 ან 409)
public sealed class SmartSchemaModelValidator : AbstractValidator<StsSmartSchemaDataModel>
{
    public SmartSchemaModelValidator()
    {
        RuleFor(x => x.Name).RequiredWithMaxLength(_ => nameof(StsSmartSchemaDataModel.Name),
            SmartSchema.NameMaxLength);

        //ცარიელი სია დასაშვებია (სქემა მხოლოდ ბოლო ფაილებს ინახავს), მაგრამ არა null
        RuleFor(x => x.Details).NotNull().WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValueRequired))
            .WithMessage(x => SupportToolsServerApiClientErrors
                .ValueRequired(ValueName(x, nameof(StsSmartSchemaDataModel.Details))).Description);

        RuleForEach(x => x.Details).SetValidator(new SmartSchemaDetailModelValidator());

        //კლიენტი დეტალებს პერიოდის ტიპით ეძებს, ამიტომ ტიპი სქემაში ერთხელ უნდა შეგვხვდეს
        RuleFor(x => x.Details).Must(x => x is null || UniqueValues.AreUnique(x.Select(y => y.PeriodType)))
            .WithErrorCode(nameof(SupportToolsServerApiClientErrors.ValuesNotUnique)).WithMessage(x =>
                SupportToolsServerApiClientErrors
                    .ValuesNotUnique(ValueName(x, SmartSchemaDetailModelValidator.PeriodTypeValueName)).Description);
    }

    private static string ValueName(StsSmartSchemaDataModel smartSchema, string propertyName)
    {
        return string.IsNullOrWhiteSpace(smartSchema.Name) ? propertyName : $"{smartSchema.Name}.{propertyName}";
    }
}
