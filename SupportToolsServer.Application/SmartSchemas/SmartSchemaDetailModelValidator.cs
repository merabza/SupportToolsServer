using FluentValidation;
using SupportToolsServer.Application.Validation;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.SmartSchemas;

//ჭკვიანი სქემის დეტალის წესები. სერვერი კლიენტის EPeriodType-ს არ იცნობს, ამიტომ პერიოდის ტიპი მხოლოდ სიგრძით
//მოწმდება (SmartSchemaDetailConfiguration-ის HasMaxLength). PreserveCount-ს წესი არ აქვს, კლიენტშიც არ აქვს
public sealed class SmartSchemaDetailModelValidator : AbstractValidator<StsSmartSchemaDetailDataModel>
{
    public const string PeriodTypeValueName =
        $"{nameof(StsSmartSchemaDataModel.Details)}.{nameof(StsSmartSchemaDetailDataModel.PeriodType)}";

    public SmartSchemaDetailModelValidator()
    {
        RuleFor(x => x.PeriodType).RequiredWithMaxLength(_ => PeriodTypeValueName,
            SmartSchemaDetail.PeriodTypeMaxLength);
    }
}
