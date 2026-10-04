using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DotnetTools;

namespace SupportToolsServer.Application.DotnetTools;

internal static class DotnetToolContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "DotnetTool";

    public static StsDotnetToolDataModel ToContractModel(this DotnetTool dotnetTool)
    {
        return new StsDotnetToolDataModel
        {
            Name = dotnetTool.Name,
            PackageId = dotnetTool.PackageId,
            MaxVersion = dotnetTool.MaxVersion,
            Description = dotnetTool.Description,
            Version = dotnetTool.Version
        };
    }
}
