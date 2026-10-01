using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//სამუშაო ფოლდერის Gits ქვეფოლდერში რეპოზიტორიის დაკლონვა ან განახლება,
//როგორც SupportTools-ის Update Git Project ოპერაციაში (ProcessFolder-ის გარეშე)
public sealed record UpdateGitProjectCommand(
    string GitProjectName,
    string GitProjectAddress,
    string GitProjectFolderName) : ICommand;
