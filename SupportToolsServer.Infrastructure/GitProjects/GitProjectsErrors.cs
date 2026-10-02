using SystemTools.SharedKernel;

namespace SupportToolsServer.Infrastructure.GitProjects;

public static class GitProjectsErrors
{
    public static readonly Error WorkFolderIsNotSpecified = Error.Problem(nameof(WorkFolderIsNotSpecified),
        "AppOptions:WorkFolder is not specified");

    public static Error CannotCreateFolder(string folderName)
    {
        return Error.Problem(nameof(CannotCreateFolder), $"Folder {folderName} does not exist and cannot be created");
    }

    public static Error FolderIsOutsideGitsFolder(string folderName)
    {
        return Error.Problem(nameof(FolderIsOutsideGitsFolder), $"Folder {folderName} is not inside the Gits folder");
    }
}
