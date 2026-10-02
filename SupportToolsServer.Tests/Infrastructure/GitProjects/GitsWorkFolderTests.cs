using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Infrastructure.Options;
using SupportToolsServer.Tests.TestInfrastructure;
using SystemTools.SharedKernel;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace SupportToolsServer.Tests.Infrastructure.GitProjects;

public sealed class GitsWorkFolderTests : IDisposable
{
    private readonly TempFolder _temp = new();

    public void Dispose()
    {
        _temp.Dispose();
    }

    private static GitsWorkFolder Create(string? workFolder)
    {
        return new GitsWorkFolder(MsOptions.Create(new AppOptions { WorkFolder = workFolder }),
            NullLogger<GitsWorkFolder>.Instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetProjectFolderPath_ReturnsWorkFolderIsNotSpecified_WhenTheWorkFolderIsEmpty(string? workFolder)
    {
        Result<string> result = Create(workFolder).GetProjectFolderPath("RepoA");

        Assert.Equal(GitProjectsErrors.WorkFolderIsNotSpecified, result.Error);
    }

    [Fact]
    public void GetProjectFolderPath_CreatesTheWorkAndGitsFolders_AndReturnsTheProjectPath()
    {
        string workFolder = _temp.Combine("Work");

        Result<string> result = Create(workFolder).GetProjectFolderPath("RepoA");

        Assert.Equal(Path.Combine(workFolder, "Gits", "RepoA"), result.Value);
        Assert.True(Directory.Exists(Path.Combine(workFolder, "Gits")));
        Assert.False(Directory.Exists(result.Value));
    }

    [Fact]
    public void GetProjectFolderPath_ReplacesBothSeparatorsOfTheFolderNameWithDots()
    {
        string workFolder = _temp.Combine("Work");

        Result<string> result = Create(workFolder).GetProjectFolderPath(@"Group\Sub/RepoA");

        Assert.Equal(Path.Combine(workFolder, "Gits", "Group.Sub.RepoA"), result.Value);
    }

    //Flattened, "..\../x" stays inside the Gits folder as "......x"
    [Fact]
    public void GetProjectFolderPath_KeepsAFlattenedParentPathInsideTheGitsFolder()
    {
        string workFolder = _temp.Combine("Work");

        Result<string> result = Create(workFolder).GetProjectFolderPath(@"..\../x");

        Assert.Equal(Path.Combine(workFolder, "Gits", "......x"), result.Value);
    }

    //"." is the Gits folder itself and ".." the work folder around it
    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public void GetProjectFolderPath_RejectsAFolderThatIsNotInsideTheGitsFolder(string folderName)
    {
        Result<string> result = Create(_temp.Combine("Work")).GetProjectFolderPath(folderName);

        Assert.Equal(GitProjectsErrors.FolderIsOutsideGitsFolder(folderName), result.Error);
    }

    //On Windows "C:" makes a drive-relative path, "a:" a path on drive A, and the trailing dots and spaces of the
    //last segment are trimmed, so "..." and ".. " resolve to the Gits folder itself
    [Theory]
    [InlineData(@"C:\Windows")]
    [InlineData("a:b")]
    [InlineData("...")]
    [InlineData(".. ")]
    public void GetProjectFolderPath_RejectsAFolderThatWindowsResolvesOutsideTheGitsFolder(string folderName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Result<string> result = Create(_temp.Combine("Work")).GetProjectFolderPath(folderName);

        Assert.Equal(GitProjectsErrors.FolderIsOutsideGitsFolder(folderName), result.Error);
    }

    [Fact]
    public void GetProjectFolderPath_ReturnsCannotCreateFolder_WhenTheWorkFolderIsAFile()
    {
        string workFolder = _temp.Combine("Work");
        File.WriteAllText(workFolder, "file");

        Result<string> result = Create(workFolder).GetProjectFolderPath("RepoA");

        Assert.Equal(GitProjectsErrors.CannotCreateFolder(workFolder), result.Error);
    }

    [Fact]
    public void GetProjectFolderPath_ReturnsCannotCreateFolder_WhenTheGitsFolderIsAFile()
    {
        string workFolder = _temp.Combine("Work");
        Directory.CreateDirectory(workFolder);
        string gitsFolder = Path.Combine(workFolder, "Gits");
        File.WriteAllText(gitsFolder, "file");

        Result<string> result = Create(workFolder).GetProjectFolderPath("RepoA");

        Assert.Equal(GitProjectsErrors.CannotCreateFolder(gitsFolder), result.Error);
    }

    [Fact]
    public void Exists_ReturnsWhetherTheFolderExists()
    {
        GitsWorkFolder sut = Create(_temp.Path);

        Assert.True(sut.Exists(_temp.Path));
        Assert.False(sut.Exists(_temp.Combine("Missing")));
    }

    [Fact]
    public void Delete_RemovesTheFolderWithReadOnlyFilesInSubfolders()
    {
        string projectFolder = _temp.Combine("RepoA");
        string objectsFolder = Path.Combine(projectFolder, ".git", "objects");
        Directory.CreateDirectory(objectsFolder);
        string readOnlyFile = Path.Combine(objectsFolder, "pack");
        File.WriteAllText(readOnlyFile, "object");
        File.SetAttributes(readOnlyFile, FileAttributes.ReadOnly);

        Create(_temp.Path).Delete(projectFolder);

        Assert.False(Directory.Exists(projectFolder));
    }
}
