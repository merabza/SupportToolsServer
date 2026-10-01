using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitRepos.UpdateGitProject;

//სამუშაო ფოლდერის Gits ქვეფოლდერი, სადაც რეპოზიტორიები იკლონება
public interface IGitsWorkFolder
{
    //სამუშაო და Gits ფოლდერებს საჭიროების შემთხვევაში ქმნის და პროექტის ფოლდერის სრულ გზას აბრუნებს
    Result<string> GetProjectFolderPath(string gitProjectFolderName);

    bool Exists(string projectFolderPath);

    void Delete(string projectFolderPath);
}
