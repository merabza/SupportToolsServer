using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.Runtimes.UpdateRuntime;

//upsert ვერსიით (CLAUDE.md, Registry conventions). Runtime.Version მოსალოდნელი ვერსიაა, Runtime.Name-ს კი
//ენდპოინტი მისამართის key-ით ავსებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateRuntimeCommand : ICommand<int>
{
    public UpdateRuntimeCommand(StsRuntimeDataModel runtime)
    {
        Runtime = runtime;
    }

    public StsRuntimeDataModel Runtime { get; }
}
