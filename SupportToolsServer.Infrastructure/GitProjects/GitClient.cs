using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared.Errors;

namespace SupportToolsServer.Infrastructure.GitProjects;

//SupportTools-ის GitProcessor-ის ის ნაწილი, რომელიც Update Git Project ოპერაციას სჭირდება.
//სერვერზე კონსოლი არ გამოიყენება, გამოსავალი და შეცდომები ლოგში იწერება
public sealed class GitClient : IGitClient
{
    private const string Git = "git";

    private readonly ILogger<GitClient> _logger;
    private readonly Func<IReadOnlyList<string>, Result<string>> _runGit;

    public GitClient(ILogger<GitClient> logger)
    {
        _logger = logger;
        _runGit = RunGit;
    }

    //ტესტებისთვის: git არ ეშვება, ბრძანების არგუმენტების სია runGit-ს გადაეცემა
    internal GitClient(Func<IReadOnlyList<string>, Result<string>> runGit)
    {
        _logger = NullLogger<GitClient>.Instance;
        _runGit = runGit;
    }

    public Result<string> GetRemoteOriginUrl(string projectFolderPath)
    {
        return RunWithOutput(projectFolderPath, "config", "--get", "remote.origin.url");
    }

    public Result Clone(string gitProjectAddress, string projectFolderPath)
    {
        //"--"-ის შემდეგ "-"-ით დაწყებული მისამართიც რეპოზიტორიად აღიქმება და არა ოფციად
        return Run(["clone", "--", gitProjectAddress, projectFolderPath]);
    }

    public Result<bool> HasChanges(string projectFolderPath)
    {
        Result<string> statusResult = RunWithOutput(projectFolderPath, "status", "--porcelain");
        return statusResult.IsFailure ? statusResult.Error : !string.IsNullOrEmpty(statusResult.Value);
    }

    public Result Restore(string projectFolderPath)
    {
        //შემდეგი 3 ბრძანება თანმიმდევრობით იგივეა, რაც პროექტის თავიდან დაკლონვა, ოღონდ მინიმალური ჩამოტვირთვით
        foreach (string[] arguments in (string[][])[["reset"], ["checkout", "."], ["clean", "-fdx"]])
        {
            Result result = RunInFolder(projectFolderPath, arguments);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    public Result RemoteUpdate(string projectFolderPath)
    {
        return RunInFolder(projectFolderPath, "remote", "update");
    }

    //https://newbedev.com/check-if-pull-needed-in-git
    public Result<bool> NeedPull(string projectFolderPath)
    {
        Result<string> localResult = RunWithOutput(projectFolderPath, "rev-parse", "@");
        if (localResult.IsFailure)
        {
            return localResult.Error;
        }

        Result<string> remoteResult = RunWithOutput(projectFolderPath, "rev-parse", "@{u}");
        if (remoteResult.IsFailure)
        {
            return remoteResult.Error;
        }

        Result<string> baseResult = RunWithOutput(projectFolderPath, "merge-base", "@", "@{u}");
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
        return RunInFolder(projectFolderPath, "pull");
    }

    //არგუმენტები ArgumentList-ით, თითო-თითოდ გადაეცემა: ბრჭყალის ან გამოტოვების შემცველი მისამართი თუ გზა
    //ერთ არგუმენტად რჩება და ახალ ოფციად ვერ გაიყოფა
    internal static ProcessStartInfo CreateStartInfo(IReadOnlyList<string> arguments)
    {
        //git PATH-იდან ეშვება, როგორც მანამდე StShared.RunProcessWithOutput-ით
#pragma warning disable S4036
        var startInfo = new ProcessStartInfo(Git)
#pragma warning restore S4036
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private Result RunInFolder(string projectFolderPath, params string[] arguments)
    {
        return Run(["-C", projectFolderPath, .. arguments]);
    }

    private Result Run(IReadOnlyList<string> arguments)
    {
        Result<string> result = _runGit(arguments);
        return result.IsFailure ? result.Error : Result.Success();
    }

    private Result<string> RunWithOutput(string projectFolderPath, params string[] arguments)
    {
        Result<string> result = _runGit(["-C", projectFolderPath, .. arguments]);
        return result.IsFailure ? result.Error : result.Value.Trim(Environment.NewLine.ToCharArray());
    }

    //SystemTools-ის StShared.RunProcessWithOutput-ის ანალოგი, რომელიც არგუმენტებს მხოლოდ ერთ სტრიქონად იღებს
    private Result<string> RunGit(IReadOnlyList<string> arguments)
    {
        //ბრძანების ტექსტი მხოლოდ ლოგისა და შეცდომის აღწერისთვისაა
        string commandLine = $"{Git} {string.Join(' ', arguments)}";
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Running {CommandLine}", commandLine);
        }

        using var process = new Process();
        process.StartInfo = CreateStartInfo(arguments);
        var errorOutputBuilder = new StringBuilder();
        //stderr ასინქრონულად იკითხება, რომ სავსე pipe-ზე პროცესი არ გაიჭედოს
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                errorOutputBuilder.AppendLine(e.Data);
            }
        };
        process.Start();
        process.BeginErrorReadLine();
        string output = process.StandardOutput.ReadToEnd();
        //უპარამეტრო WaitForExit stderr-ის ასინქრონული კითხვის დასრულებასაც ელოდება
        process.WaitForExit();
        string errorOutput = errorOutputBuilder.ToString().TrimEnd();

        if (process.ExitCode == 0)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Output for '{CommandLine}' is{NewLine}{Output}", commandLine,
                    Environment.NewLine, output);
                if (errorOutput.Length > 0)
                {
                    _logger.LogInformation("Error output for '{CommandLine}' is{NewLine}{ErrorOutput}", commandLine,
                        Environment.NewLine, errorOutput);
                }
            }

            return output;
        }

        string errorMessage =
            $"{commandLine} process was finished with errors. ExitCode={process.ExitCode}{(errorOutput.Length == 0 ? string.Empty : $"{Environment.NewLine}{errorOutput}")}";
        _logger.LogError("{ErrorMessage}", errorMessage);
        return SystemToolsErrors.RunProcessError(errorMessage);
    }
}
