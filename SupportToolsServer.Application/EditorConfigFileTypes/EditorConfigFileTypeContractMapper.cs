using System.Collections.Generic;
using System.Linq;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;

namespace SupportToolsServer.Application.EditorConfigFileTypes;

internal static class EditorConfigFileTypeContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordIsInUse, ReferencedRecordsNotFound)
    public const string EntityName = "EditorConfigFileType";

    //სხვა აგრეგატები .editorconfig შაბლონს Id-ით ინახავენ, კონტრაქტში კი მის სახელს გადასცემენ
    //(StsProjectDataModel.EditorConfigPatternName)
    public static Dictionary<EditorConfigFileTypeId, string> ToNamesById(
        this IEnumerable<EditorConfigFileType> editorConfigFileTypes)
    {
        return editorConfigFileTypes.ToDictionary(x => x.Id, x => x.Name);
    }
}
