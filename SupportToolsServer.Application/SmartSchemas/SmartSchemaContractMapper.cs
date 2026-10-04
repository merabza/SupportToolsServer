using System;
using System.Linq;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.SmartSchemas;

namespace SupportToolsServer.Application.SmartSchemas;

internal static class SmartSchemaContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "SmartSchema";

    //დეტალების რიგს მნიშვნელობა არ აქვს, ამიტომ ისინი პერიოდის ტიპით ლაგდება: კლიენტის ჰეში რიგზე არ უნდა იყოს
    //დამოკიდებული
    public static StsSmartSchemaDataModel ToContractModel(this SmartSchema smartSchema)
    {
        return new StsSmartSchemaDataModel
        {
            Name = smartSchema.Name,
            LastPreserveCount = smartSchema.LastPreserveCount,
            Details =
            [
                .. smartSchema.Details.OrderBy(x => x.PeriodType, StringComparer.OrdinalIgnoreCase).Select(x =>
                    new StsSmartSchemaDetailDataModel { PeriodType = x.PeriodType, PreserveCount = x.PreserveCount })
            ],
            Version = smartSchema.Version
        };
    }
}
