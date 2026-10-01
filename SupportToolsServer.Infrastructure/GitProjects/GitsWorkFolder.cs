using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.Options;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportToolsServer.Infrastructure.GitProjects;

//SupportTools-ის GitFolderCountHelper-ის ანალოგი
public sealed class GitsWorkFolder : IGitsWorkFolder
{
    private const string GitsFolderName = "Gits";
    private readonly IOptions<AppOptions> _appOptions;

    private readonly ILogger<GitsWorkFolder> _logger;

    public GitsWorkFolder(IOptions<AppOptions> appOptions, ILogger<GitsWorkFolder> logger)
    {
        _appOptions = appOptions;
        _logger = logger;
    }

    public Result<string> GetProjectFolderPath(string gitProjectFolderName)
    {
        string? workFolder = _appOptions.Value.WorkFolder;
        if (string.IsNullOrWhiteSpace(workFolder))
        {
            return GitProjectsErrors.WorkFolderIsNotSpecified;
        }

        //შემოწმდეს სამუშაო ფოლდერი და მასში Gits ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
        if (FileStat.CreateFolderIfNotExists(workFolder, false, _logger) is null)
        {
            return GitProjectsErrors.CannotCreateFolder(workFolder);
        }

        string gitsFolder = Path.Combine(workFolder, GitsFolderName);
        if (FileStat.CreateFolderIfNotExists(gitsFolder, false, _logger) is null)
        {
            return GitProjectsErrors.CannotCreateFolder(gitsFolder);
        }

        return Path.Combine(gitsFolder, gitProjectFolderName.Replace($"{Path.DirectorySeparatorChar}", "."));
    }

    public bool Exists(string projectFolderPath)
    {
        return Directory.Exists(projectFolderPath);
    }

    public void Delete(string projectFolderPath)
    {
        //.git ფოლდერის ზოგიერთი ფაილი მხოლოდ წასაკითხია და Directory.Delete მას ვერ წაშლიდა
        foreach (FileInfo file in new DirectoryInfo(projectFolderPath).EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        Directory.Delete(projectFolderPath, true);
    }
}
