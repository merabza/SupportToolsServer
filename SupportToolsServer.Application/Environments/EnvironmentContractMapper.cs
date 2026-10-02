using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.DeploymentEnvironments;

namespace SupportToolsServer.Application.Environments;

internal static class EnvironmentContractMapper
{
    //ჩანაწერის ტიპის სახელი რეესტრის შეცდომებში (RecordWithNameNotFound, ConcurrencyConflict, RecordIsInUse)
    public const string EntityName = "Environment";

    public static StsEnvironmentDataModel ToContractModel(this DeploymentEnvironment environment)
    {
        return new StsEnvironmentDataModel
        {
            Name = environment.Name, Description = environment.Description, Version = environment.Version
        };
    }
}
