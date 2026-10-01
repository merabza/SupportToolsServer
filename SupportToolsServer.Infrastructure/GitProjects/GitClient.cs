using System;
using Microsoft.Extensions.Logging;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportToolsServer.Infrastructure.GitProjects;

//SupportTools-ის GitProcessor-ის ის ნაწილი, რომელიც Update Git Project ოპერაციას სჭირდება.
//სერვერზე კონსოლი არ გამოიყენება, გამოსავალი და შეცდომები ლოგში იწერება
public sealed class GitClient : IGitClient
{
    private const string Git = "git";

    private readonly ILogger<GitClient> _logger;

    public GitClient(ILogger<GitClient> logger)
    {
        _logger = logger;
    }

    public Result<string> GetRemoteOriginUrl(string projectFolderPath)
    {
        return RunWithOutput(projectFolderPath, "config --get remote.origin.url");
    }

    public Result Clone(string gitProjectAddress, string projectFolderPath)
    {
        return Run($"clone {gitProjectAddress} \"{projectFolderPath}\"");
    }

    public Result<bool> HasChanges(string projectFolderPath)
    {
        Result<string> statusResult = RunWithOutput(projectFolderPath, "status --porcelain");
        return statusResult.IsFailure ? statusResult.Error : !string.IsNullOrEmpty(statusResult.Value);
    }

    public Result Restore(string projectFolderPath)
    {
        //შემდეგი 3 ბრძანება თანმიმდევრობით იგივეა, რაც პროექტის თავიდან დაკლონვა, ოღონდ მინიმალური ჩამოტვირთვით
        foreach (string arguments in (string[])["reset", "checkout .", "clean -fdx"])
        {
            Result result = Run(projectFolderPath, arguments);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    public Result RemoteUpdate(string projectFolderPath)
    {
        return Run(projectFolderPath, "remote update");
    }

    //https://newbedev.com/check-if-pull-needed-in-git
    public Result<bool> NeedPull(string projectFolderPath)
    {
        Result<string> localResult = RunWithOutput(projectFolderPath, "rev-parse @");
        if (localResult.IsFailure)
        {
            return localResult.Error;
        }

        Result<string> remoteResult = RunWithOutput(projectFolderPath, "rev-parse @{u}");
        if (remoteResult.IsFailure)
        {
            return remoteResult.Error;
        }

        Result<string> baseResult = RunWithOutput(projectFolderPath, "merge-base @ @{u}");
        if (baseResult.IsFailure)
        {
            return baseResult.Error;
        }

        string local = localResult.Value;
        string remote = remoteResult.Value;

        //up to date ან need to push: pull საჭირო არ არის. need to pull ან diverged: pull საჭიროა
        if (local == remote || remote == baseResult.Value)
        {
            return false;
        }

        return true;
    }

    public Result Pull(string projectFolderPath)
    {
        return Run(projectFolderPath, "pull");
    }

    private Result Run(string projectFolderPath, string arguments)
    {
        return Run($"-C \"{projectFolderPath}\" {arguments}");
    }

    private Result Run(string arguments)
    {
        Result<(string, int)> result = StShared.RunProcessWithOutput(false, _logger, Git, arguments);
        return result.IsFailure ? result.Error : Result.Success();
    }

    private Result<string> RunWithOutput(string projectFolderPath, string arguments)
    {
        Result<(string, int)> result =
            StShared.RunProcessWithOutput(false, _logger, Git, $"-C \"{projectFolderPath}\" {arguments}");
        return result.IsFailure ? result.Error : result.Value.Item1.Trim(Environment.NewLine.ToCharArray());
    }
}
