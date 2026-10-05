using System.Collections.Generic;
using System.Linq;

namespace SupportToolsServer.Application.Registry;

//ველები, რომლებიც სხვა აგრეგატის ჩანაწერს Id-ით ინახავს, კონტრაქტში კი მის სახელს გადასცემს (CLAUDE.md, Registry
//conventions)
internal static class ReferenceFields
{
    //მითითებული ჩანაწერის სახელი კონტრაქტისთვის. null ნიშნავს, რომ მითითება არ არის
    public static string? GetName<TId>(this IReadOnlyDictionary<TId, string> names, TId? id) where TId : class
    {
        return id is null ? null : names[id];
    }

    //ველები, რომლებიც id-ს მიმართავს, "<აგრეგატი>.<ველი>" ფორმით, მოცემული რიგით: singleton-ის მომხმარებლები
    //409 RecordIsInUse-ისთვის. singleton-ს სახელი არ აქვს, ამიტომ მომხმარებელს მისი ველი ასახელებს
    public static IEnumerable<string> Usages<TId>(string aggregateName, TId id,
        params (string Field, TId? Value)[] fields) where TId : class
    {
        return fields.Where(x => id.Equals(x.Value)).Select(x => $"{aggregateName}.{x.Field}");
    }
}
