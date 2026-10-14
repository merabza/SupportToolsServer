using SystemTools.SharedKernel;

namespace SupportToolsServer.Infrastructure.GitProjects;

public static class GitProjectsErrors
{
    public static readonly Error WorkFolderIsNotSpecified = Error.Problem(nameof(WorkFolderIsNotSpecified),
        "AppOptions:WorkFolder is not specified");

    //git ls-remote-მა HEAD-ის branch ან commit არ დააბრუნა (მაგალითად, ცარიელი რეპოზიტორია)
    public static readonly Error RemoteHeadIsNotFound = Error.Problem(nameof(RemoteHeadIsNotFound),
        "Remote repository has no HEAD branch");

    public static Error CannotCreateFolder(string folderName)
    {
        return Error.Problem(nameof(CannotCreateFolder), $"Folder {folderName} does not exist and cannot be created");
    }

    public static Error FolderIsOutsideGitsFolder(string folderName)
    {
        return Error.Problem(nameof(FolderIsOutsideGitsFolder), $"Folder {folderName} is not inside the Gits folder");
    }

    //კლონის ფოლდერის ჩამონათვალი ვერ წაიკითხა (წვდომა, IO)
    public static Error FolderCannotBeScanned(string folderPath, string message)
    {
        return Error.Problem(nameof(FolderCannotBeScanned), $"Folder {folderPath} cannot be scanned: {message}");
    }

    //პროექტის ფაილი ვერ წაიკითხა ან ის სწორი XML არ არის (DTD-ის შემცველი ფაილიც)
    public static Error ProjectFileCannotBeRead(string filePath, string message)
    {
        return Error.Problem(nameof(ProjectFileCannotBeRead), $"Project file {filePath} cannot be read: {message}");
    }
}
