using SupportToolsServerApiContracts.Models;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.DotnetTools.UpdateDotnetTool;

//upsert ვერსიით (CLAUDE.md, Registry conventions). DotnetTool.Version მოსალოდნელი ვერსიაა, DotnetTool.Name-ს კი
//ენდპოინტი მისამართის key-ით ავსებს. პასუხი ჩანაწერის ახალი ვერსიაა
public sealed class UpdateDotnetToolCommand : ICommand<int>
{
    public UpdateDotnetToolCommand(StsDotnetToolDataModel dotnetTool)
    {
        DotnetTool = dotnetTool;
    }

    public StsDotnetToolDataModel DotnetTool { get; }
}
