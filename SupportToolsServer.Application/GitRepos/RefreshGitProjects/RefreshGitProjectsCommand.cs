using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.RefreshGitProjects;

//ყველა რეპოზიტორიის განახლება (pull და პროექტების სკანირება) რიგში დგება, რომ სერვერის კლონები და GitProjects ახალი
//იყოს (B9). მას GitProjectsRefreshBackgroundService სტარტზე და შემდეგ პერიოდულად უშვებს
public sealed record RefreshGitProjectsCommand : ICommand;
