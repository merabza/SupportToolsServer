using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Tests.TestInfrastructure;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.GitProjects;

//The clone is a folder of a temporary Gits folder, so the relative paths start with its name, as in the client's
//Update Git Projects (GitProjectsUpdater)
public sealed class GitProjectFilesScannerTests : IDisposable
{
    private readonly string _clonePath;
    private readonly GitProjectFilesScanner _sut = new();
    private readonly TempFolder _temp = new();

    public GitProjectFilesScannerTests()
    {
        _clonePath = _temp.Combine("Gits", "RepoA");
        Directory.CreateDirectory(_clonePath);
    }

    public void Dispose()
    {
        _temp.Dispose();
    }

    private void Write(string relativePath, string content)
    {
        ProjectFiles.Write(_clonePath, relativePath, content);
    }

    private List<ScannedGitProject> Scan()
    {
        Result<List<ScannedGitProject>> result = _sut.Scan(_clonePath);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static List<(string, string, string)> Describe(IEnumerable<ScannedGitProject> scannedGitProjects)
    {
        return
        [
            .. scannedGitProjects.Select(x =>
                (x.ProjectRelativePath, x.ProjectFileName, string.Join(",", x.DependsOnProjectNames)))
        ];
    }

    //Like the client, the subfolders come first and then the csproj and the esproj files of the folder. The paths use \
    //on every operating system and start with the folder of the clone, a project at the root has only that folder
    [Fact]
    public void Scan_FindsTheProjectFilesOfEveryFolder_WithPathsRelativeToTheGitsFolder()
    {
        Write("RepoA.csproj", ProjectFiles.Project(@"AppA\AppA.csproj"));
        Write(Path.Combine("AppA", "AppA.csproj"),
            ProjectFiles.Project(@"..\Libs\Nested\LibN\LibN.csproj", @"..\..\..\SystemTools\Kernel\Kernel.csproj"));
        Write(Path.Combine("Libs", "Nested", "LibN", "LibN.csproj"), ProjectFiles.Project());
        Write(Path.Combine("Front", "front.esproj"), ProjectFiles.Project());
        Write(Path.Combine("Front", "Front.Server.csproj"), ProjectFiles.Project(@"front.esproj"));
        Write(Path.Combine("Docs", "readme.md"), "not a project");

        List<ScannedGitProject> scanned = Scan();

        Assert.Equal([
            (@"RepoA\AppA", "AppA.csproj", "LibN,Kernel"), (@"RepoA\Front", "Front.Server.csproj", "front"),
            (@"RepoA\Front", "front.esproj", ""), (@"RepoA\Libs\Nested\LibN", "LibN.csproj", ""),
            ("RepoA", "RepoA.csproj", "AppA")
        ], Describe(scanned));
    }

    [Fact]
    public void Scan_ReturnsNoProjects_ForACloneWithoutProjectFiles()
    {
        Write(Path.Combine("src", "index.js"), "export {}");
        Write("package.json", "{}");

        Assert.Empty(Scan());
    }

    //The client skips only the .git folder: other folders whose names start with a dot, and hidden folders, are scanned
    [Fact]
    public void Scan_SkipsTheGitFolderOnly()
    {
        Write(Path.Combine(".git", "Fake", "Fake.csproj"), ProjectFiles.Project());
        Write(Path.Combine(".github", "Tool", "Tool.csproj"), ProjectFiles.Project());
        Write(Path.Combine("Hidden", "HiddenApp.csproj"), ProjectFiles.Project());
        var hidden = new DirectoryInfo(Path.Combine(_clonePath, "Hidden"));
        hidden.Attributes |= FileAttributes.Hidden;

        List<ScannedGitProject> scanned = Scan();

        Assert.Equal([(@"RepoA\.github\Tool", "Tool.csproj", ""), (@"RepoA\Hidden", "HiddenApp.csproj", "")],
            Describe(scanned));
    }

    //A linked folder may lead out of the clone or back into it, so the scanner does not follow it. A Windows clone,
    //like the client's, has no linked folders: git writes a link as a file there
    [Fact]
    public void Scan_DoesNotFollowALinkedFolder()
    {
        string outside = _temp.Combine("Outside");
        string linkPath = Path.Combine(_clonePath, "Linked");
        ProjectFiles.Write(outside, Path.Combine("LibX", "LibX.csproj"), ProjectFiles.Project());
        ProjectFiles.CreateLinkedFolder(linkPath, outside);
        Write(Path.Combine("AppA", "AppA.csproj"), ProjectFiles.Project());
        try
        {
            List<ScannedGitProject> scanned = Scan();

            Assert.True(File.Exists(Path.Combine(linkPath, "LibX", "LibX.csproj")));
            Assert.Equal([(@"RepoA\AppA", "AppA.csproj", "")], Describe(scanned));
        }
        finally
        {
            //Without a privilege the recursive delete of the temporary folder fails on a junction, while the link
            //alone can be removed
            Directory.Delete(linkPath);
        }
    }

    //As on Windows, the extension of a project file matches without case
    [Fact]
    public void Scan_MatchesTheExtensionWithoutCase()
    {
        Write(Path.Combine("AppA", "AppA.CSPROJ"), ProjectFiles.Project());
        Write(Path.Combine("Front", "front.EsProj"), ProjectFiles.Project());

        List<ScannedGitProject> scanned = Scan();

        Assert.Equal([(@"RepoA\AppA", "AppA.CSPROJ", ""), (@"RepoA\Front", "front.EsProj", "")], Describe(scanned));
    }

    //A dependency is the file name without extension of the full path of the Include, in the order of the document:
    //.. is resolved first, / and \ both separate, a path out of the repository or an absolute one counts as well, and
    //a name that repeats, also in another case, is written once
    [Fact]
    public void Scan_GivesTheFileNameWithoutExtensionOfEveryReferenceOnce()
    {
        Write(Path.Combine("AppA", "AppA.csproj"), ProjectFiles.Project(@"..\LibA\LibA.csproj", "../LibB/LibB.csproj",
            @"..\LibC\Old\..\LibC.csproj", @"..\..\..\Other\LibD\LibD.csproj", @"..\LibA\LibA.csproj",
            @"..\liba\LIBA.csproj", Path.Combine(_temp.Path, "Elsewhere", "LibE.csproj"), @"..\Front\front.esproj"));

        List<ScannedGitProject> scanned = Scan();

        Assert.Equal(["LibA", "LibB", "LibC", "LibD", "LibE", "front"],
            Assert.Single(scanned).DependsOnProjectNames);
    }

    //Like the client's Descendants("ItemGroup").Descendants("ProjectReference"): a reference outside an ItemGroup is
    //not read, one in an ItemGroup of a Choose or a Target is
    [Fact]
    public void Scan_ReadsTheProjectReferencesOfItemGroupsOnly()
    {
        Write(Path.Combine("AppA", "AppA.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ProjectReference Include="..\NotInItemGroup\NotInItemGroup.csproj" />
              <ItemGroup>
                <PackageReference Include="Serilog" />
                <ProjectReference Include="..\LibA\LibA.csproj" />
              </ItemGroup>
              <Choose>
                <When Condition="'$(Configuration)' == 'Debug'">
                  <ItemGroup>
                    <ProjectReference Include="..\LibB\LibB.csproj" />
                  </ItemGroup>
                </When>
              </Choose>
              <Target Name="Extra">
                <ItemGroup>
                  <ProjectReference Include="..\LibC\LibC.csproj" />
                </ItemGroup>
              </Target>
            </Project>
            """);

        List<ScannedGitProject> scanned = Scan();

        Assert.Equal(["LibA", "LibB", "LibC"], Assert.Single(scanned).DependsOnProjectNames);
    }

    //The client looks for the names without a namespace, so an old-style project in the MSBuild namespace has no
    //dependencies there either
    [Fact]
    public void Scan_FindsNoDependencies_InAProjectOfTheMsBuildNamespace()
    {
        Write(Path.Combine("OldApp", "OldApp.csproj"), """
            <?xml version="1.0" encoding="utf-8"?>
            <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemGroup>
                <ProjectReference Include="..\LibA\LibA.csproj" />
              </ItemGroup>
            </Project>
            """);

        List<ScannedGitProject> scanned = Scan();

        Assert.Equal([(@"RepoA\OldApp", "OldApp.csproj", "")], Describe(scanned));
    }

    //The XML is read without a DTD: a project file with one is an error, its entities are never expanded
    [Fact]
    public void Scan_ReturnsAnError_ForAProjectFileWithADtd()
    {
        string filePath = Path.Combine(_clonePath, "AppA", "AppA.csproj");
        Write(Path.Combine("AppA", "AppA.csproj"), """
            <?xml version="1.0"?>
            <!DOCTYPE Project [<!ENTITY lib "..\LibA\LibA.csproj">]>
            <Project><ItemGroup><ProjectReference Include="&lib;" /></ItemGroup></Project>
            """);

        Result<List<ScannedGitProject>> result = _sut.Scan(_clonePath);

        Assert.True(result.IsFailure);
        Assert.Equal(nameof(GitProjectsErrors.ProjectFileCannotBeRead), result.Error.Code);
        Assert.StartsWith($"Project file {filePath} cannot be read: ", result.Error.Description,
            StringComparison.Ordinal);
        Assert.Contains("DTD", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Scan_ReturnsAnError_ForAProjectFileThatIsNotXml()
    {
        Write(Path.Combine("AppA", "AppA.csproj"), ProjectFiles.Project());
        Write(Path.Combine("Broken", "Broken.csproj"), "<Project><ItemGroup></Project>");

        Result<List<ScannedGitProject>> result = _sut.Scan(_clonePath);

        Assert.True(result.IsFailure);
        Assert.StartsWith(
            $"Project file {Path.Combine(_clonePath, "Broken", "Broken.csproj")} cannot be read: ",
            result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Scan_ReturnsAnError_WhenTheFolderDoesNotExist()
    {
        string missing = _temp.Combine("Gits", "Missing");

        Result<List<ScannedGitProject>> result = _sut.Scan(missing);

        Assert.True(result.IsFailure);
        Assert.Equal(nameof(GitProjectsErrors.FolderCannotBeScanned), result.Error.Code);
        Assert.StartsWith($"Folder {missing} cannot be scanned: ", result.Error.Description,
            StringComparison.Ordinal);
    }
}
