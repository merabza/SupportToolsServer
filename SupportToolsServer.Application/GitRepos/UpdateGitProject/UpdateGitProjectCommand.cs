using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//სამუშაო ფოლდერის Gits ქვეფოლდერში რეპოზიტორიის დაკლონვა ან განახლება და მისი პროექტების სკანირება,
//როგორც SupportTools-ის Update Git Project ოპერაციაში. სკანირების შედეგი GitRepoId-ით ინახება (B9)
public sealed record UpdateGitProjectCommand(
    GitRepoId GitRepoId,
    string GitProjectName,
    string GitProjectAddress,
    string GitProjectFolderName) : ICommand;
