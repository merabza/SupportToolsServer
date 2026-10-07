using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SupportToolsServer.Tests.TestInfrastructure;

//Project files and folders for the scans of the tests; any failure is a broken test setup
internal static class ProjectFiles
{
    //An SDK project with the given project references in one ItemGroup
    public static string Project(params string[] projectReferences)
    {
        string references =
            string.Concat(projectReferences.Select(x => $"    <ProjectReference Include=\"{x}\" />\n"));
        return $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n{references}  </ItemGroup>\n</Project>\n";
    }

    //Writes the file under the folder, creating the folders it needs
    public static void Write(string folder, string relativePath, string content)
    {
        string path = Path.Combine(folder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    //A folder that links to another one. Windows needs a privilege for a symbolic link of a folder, but not for a
    //junction
    public static void CreateLinkedFolder(string linkPath, string targetPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return;
        }

        var startInfo = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string argument in (string[])["/c", "mklink", "/J", linkPath, targetPath])
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("cmd did not start");
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"mklink /J {linkPath} failed: {error}");
        }
    }
}
