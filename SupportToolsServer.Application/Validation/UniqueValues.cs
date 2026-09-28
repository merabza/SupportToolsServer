using System;
using System.Collections.Generic;
using System.Linq;

namespace SupportToolsServer.Application.Validation;

internal static class UniqueValues
{
    //შევსებული მნიშვნელობები რეგისტრის გაუთვალისწინებლად უნდა განსხვავდებოდეს, როგორც ბაზის უნიკალურ ინდექსში.
    //ცარიელ მნიშვნელობას ველის საკუთარი წესი იჭერს
    public static bool AreUnique(IEnumerable<string?> values)
    {
        List<string> filled = [.. values.OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x))];
        return filled.Count == filled.Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }
}
