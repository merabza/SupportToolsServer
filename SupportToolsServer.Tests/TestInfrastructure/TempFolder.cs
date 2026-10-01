using System;
using System.IO;

namespace SupportToolsServer.Tests.TestInfrastructure;

//A unique folder under the temp folder that is deleted (with read-only .git files) on dispose
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "StsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (!Directory.Exists(Path))
        {
            return;
        }

        foreach (FileInfo file in new DirectoryInfo(Path).EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        Directory.Delete(Path, true);
    }

    public string Combine(params string[] parts)
    {
        return System.IO.Path.Combine([Path, .. parts]);
    }
}
