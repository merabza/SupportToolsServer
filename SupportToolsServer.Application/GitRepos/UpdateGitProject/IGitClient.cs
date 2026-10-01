using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//git-ის ბრძანებები ლოკალურ რეპოზიტორიაზე, რომლის ფოლდერიც projectFolderPath-ია
public interface IGitClient
{
    Result<string> GetRemoteOriginUrl(string projectFolderPath);

    Result Clone(string gitProjectAddress, string projectFolderPath);

    //არის თუ არა ლოკალური ცვლილებები (git status --porcelain)
    Result<bool> HasChanges(string projectFolderPath);

    //git reset, git checkout . და git clean -fdx: ფოლდერი სერვერის ვერსიას უბრუნდება თავიდან დაკლონვის გარეშე
    Result Restore(string projectFolderPath);

    Result RemoteUpdate(string projectFolderPath);

    //ლოკალური ვერსია remote-ს ჩამორჩება ან მას დაშორდა
    Result<bool> NeedPull(string projectFolderPath);

    Result Pull(string projectFolderPath);
}
