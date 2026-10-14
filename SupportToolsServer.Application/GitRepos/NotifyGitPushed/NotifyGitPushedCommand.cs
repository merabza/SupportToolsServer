using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.NotifyGitPushed;

//SupportTools-მა რეპოზიტორიაში push გააკეთა
public sealed record NotifyGitPushedCommand(string Key) : ICommand;
