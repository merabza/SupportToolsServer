using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.SmartSchemas.UpdateSmartSchema;

//upsert ვერსიით (CLAUDE.md, Registry conventions). SmartSchema.Version მოსალოდნელი ვერსიაა, SmartSchema.Name-ს კი
//ენდპოინტი მისამართის key-ით ავსებს. განახლება სქემას დეტალებიანად ანაცვლებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateSmartSchemaCommand : ICommand<int>
{
    public UpdateSmartSchemaCommand(StsSmartSchemaDataModel smartSchema)
    {
        SmartSchema = smartSchema;
    }

    public StsSmartSchemaDataModel SmartSchema { get; }
}
