using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.Runtimes;

namespace SupportToolsServer.Application.Runtimes;

internal static class RuntimeContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "Runtime";

    public static StsRuntimeDataModel ToContractModel(this Runtime runtime)
    {
        return new StsRuntimeDataModel
        {
            Name = runtime.Name, Description = runtime.Description, Version = runtime.Version
        };
    }
}
