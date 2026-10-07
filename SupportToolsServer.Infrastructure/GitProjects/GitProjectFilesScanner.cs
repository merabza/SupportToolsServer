using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Infrastructure.GitProjects;

//SupportTools-ის GitProjectsUpdater.ProcessFolder-ისა და ProcessOneFile-ის ანალოგი, იგივე შედეგით: ქვეფოლდერები ჯერ
//მუშავდება, მერე ფოლდერის *.csproj და *.esproj ფაილები; .git ფოლდერი გამოტოვებულია, დამალული ფოლდერები კი არა.
//დამოკიდებულება ItemGroup-ში მდგომი ProjectReference-ის Include-ის ფაილის სახელია გაფართოების გარეშე (სახელები
//namespace-ის გარეშეა, როგორც კლიენტში). განსხვავებები: XML DTD-ის გარეშე იკითხება, ბმულ ფოლდერებს (symlink, junction)
//სკანერი არ მიჰყვება, გზები ნებისმიერ ოპერაციულ სისტემაზე \-ით ბრუნდება, Include-ში \ და / ორივე გამყოფია, ხოლო
//დამოკიდებულების სახელი რეგისტრის გაუთვალისწინებლად ერთხელ ჩაიწერება (ბაზის უნიკალური ინდექსის collation-ის გამო)
public sealed class GitProjectFilesScanner : IGitProjectFilesScanner
{
    private const string GitFolderName = ".git";
    private const char CanonicalSeparator = '\\';

    //კლიენტი ფოლდერის csproj ფაილებს esproj ფაილებზე ადრე ამუშავებს
    private static readonly string[] ProjectFilePatterns = ["*.csproj", "*.esproj"];

    //Linux-ზე წერტილით დაწყებული სახელი დამალულად ითვლება, კლიენტი კი დამალულ ფოლდერებსაც კითხულობს. ბმული ფოლდერი
    //კლონის გარეთ შეიძლება მიდიოდეს ან ციკლს ქმნიდეს
    private static readonly EnumerationOptions FolderEnumerationOptions = new()
    {
        AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = false
    };

    //ფაილის სახელის რეგისტრი, Windows-ის მსგავსად, მნიშვნელოვანი არ არის
    private static readonly EnumerationOptions FileEnumerationOptions = new()
    {
        AttributesToSkip = 0, IgnoreInaccessible = false, MatchCasing = MatchCasing.CaseInsensitive
    };

    //DTD აკრძალულია და გარე რესურსები არ იკითხება
    private static readonly XmlReaderSettings ProjectXmlReaderSettings =
        new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };

    public Result<List<ScannedGitProject>> Scan(string projectFolderPath)
    {
        var projectFolder = new DirectoryInfo(projectFolderPath);
        List<ScannedGitProject> scannedGitProjects = [];
        Result result = ScanFolder(projectFolder, projectFolder.Name, scannedGitProjects);
        return result.IsFailure ? result.Error : scannedGitProjects;
    }

    //relativePath ფოლდერის გზაა Gits-ის მიმართ, კლიენტის Path.GetRelativePath(gitsFolder, folderPath)-ის მსგავსად
    private static Result ScanFolder(DirectoryInfo folder, string relativePath,
        List<ScannedGitProject> scannedGitProjects)
    {
        List<DirectoryInfo> subFolders;
        List<FileInfo> projectFiles;
        try
        {
            subFolders =
            [
                .. folder.EnumerateDirectories("*", FolderEnumerationOptions)
                    .Where(x => !string.Equals(x.Name, GitFolderName, StringComparison.Ordinal))
                    .OrderBy(x => x.Name, StringComparer.Ordinal)
            ];
            projectFiles =
            [
                .. ProjectFilePatterns.SelectMany(pattern =>
                    folder.EnumerateFiles(pattern, FileEnumerationOptions).OrderBy(x => x.Name, StringComparer.Ordinal))
            ];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return GitProjectsErrors.FolderCannotBeScanned(folder.FullName, e.Message);
        }

        foreach (DirectoryInfo subFolder in subFolders)
        {
            Result result = ScanFolder(subFolder, $"{relativePath}{CanonicalSeparator}{subFolder.Name}",
                scannedGitProjects);
            if (result.IsFailure)
            {
                return result;
            }
        }

        foreach (FileInfo projectFile in projectFiles)
        {
            Result<List<string>> dependsOnProjectNamesResult = ReadDependsOnProjectNames(projectFile, folder.FullName);
            if (dependsOnProjectNamesResult.IsFailure)
            {
                return dependsOnProjectNamesResult.Error;
            }

            scannedGitProjects.Add(new ScannedGitProject(relativePath, projectFile.Name,
                dependsOnProjectNamesResult.Value));
        }

        return Result.Success();
    }

    private static Result<List<string>> ReadDependsOnProjectNames(FileInfo projectFile, string folderPath)
    {
        XElement projectXml;
        try
        {
            using FileStream stream = projectFile.OpenRead();
            using var reader = XmlReader.Create(stream, ProjectXmlReaderSettings);
            projectXml = XElement.Load(reader);
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            return GitProjectsErrors.ProjectFileCannotBeRead(projectFile.FullName, e.Message);
        }

        List<string> dependsOnProjectNames = [];
        foreach (XAttribute include in projectXml.Descendants("ItemGroup").Descendants("ProjectReference")
                     .Attributes("Include"))
        {
            string projectName = GetProjectName(folderPath, include.Value);
            if (!dependsOnProjectNames.Contains(projectName, StringComparer.OrdinalIgnoreCase))
            {
                dependsOnProjectNames.Add(projectName);
            }
        }

        return dependsOnProjectNames;
    }

    //კლიენტის მსგავსად Include პროექტის ფაილის ფოლდერს უერთდება და სრულ გზად ნორმალიზდება (".."), სახელი კი ბოლო
    //ნაწილის ფაილის სახელია გაფართოების გარეშე. Linux-ზე \ გამყოფი არ არის, ამიტომ ორივე გამყოფი ჯერ ერთნაირდება
    private static string GetProjectName(string folderPath, string include)
    {
        string normalizedInclude = include.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFileNameWithoutExtension(Path.GetFullPath(Path.Combine(folderPath, normalizedInclude)));
    }
}
