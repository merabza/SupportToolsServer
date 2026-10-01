using System;
using System.Diagnostics;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Prepares the git repositories of the tests; any failure is a broken test setup
internal static class GitRunner
{
    public static string Run(string workingDirectory, string arguments)
    {
        var startInfo = new ProcessStartInfo("git", $"-c user.name=Test -c user.email=test@test {arguments}")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("git did not start");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {arguments} failed: {error}");
        }

        return output.Trim();
    }
}
