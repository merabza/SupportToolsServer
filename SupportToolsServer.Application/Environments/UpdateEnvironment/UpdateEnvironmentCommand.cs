using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Environments.UpdateEnvironment;

//upsert ვერსიით (CLAUDE.md, Registry conventions). Environment.Version მოსალოდნელი ვერსიაა, Environment.Name-ს კი
//ენდპოინტი მისამართის key-ით ავსებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateEnvironmentCommand : ICommand<int>
{
    public UpdateEnvironmentCommand(StsEnvironmentDataModel environment)
    {
        Environment = environment;
    }

    public StsEnvironmentDataModel Environment { get; }
}
