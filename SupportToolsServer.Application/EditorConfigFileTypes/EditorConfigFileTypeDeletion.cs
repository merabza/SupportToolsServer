using System;
using System.Collections.Generic;
using System.Linq;
using SupportToolsServer.Application.Projects;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.Projects;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.EditorConfigFileTypes;

//.editorconfig შაბლონი მხოლოდ მაშინ იშლება, როცა მას არც ერთი პროექტი არ იყენებს (FK, Restrict). გამოყენებული შაბლონები
//ერთ 409 RecordIsInUse-ში ბრუნდება, სახელის რიგით, თითოეული თავისი პროექტებით
internal static class EditorConfigFileTypeDeletion
{
    public static Result CheckNotUsed(IEnumerable<EditorConfigFileType> editorConfigFileTypes, List<Project> projects)
    {
        ILookup<string, string> usages = editorConfigFileTypes.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .SelectMany(x => projects.GetUsages(x.Id).Select(usage => (x.Name, Usage: usage)))
            .ToLookup(x => x.Name, x => x.Usage);
        return usages.Count == 0
            ? Result.Success()
            : Result.Failure(
                SupportToolsServerApiClientErrors.RecordIsInUse(EditorConfigFileTypeContractMapper.EntityName, usages));
    }
}
