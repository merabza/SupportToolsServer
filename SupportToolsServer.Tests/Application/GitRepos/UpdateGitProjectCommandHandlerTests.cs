using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupportToolsServer.Application.GitRepos.UpdateGitProject;
using SupportToolsServer.Infrastructure.GitProjects;
using SupportToolsServer.Infrastructure.Options;
using SupportToolsServer.Tests.TestInfrastructure;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.GitRepoProjects;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace SupportToolsServer.Tests.Application.GitRepos;

public sealed class UpdateGitProjectCommandHandlerTests
{
    private const string Address = "git@github.com:test/RepoA.git";
    private const string ProjectPath = @"C:\Work\Gits\Sub.RepoA";

    private static readonly Error TestError = Error.Problem("TestError", "Test error");
    private static readonly GitRepoId RepoAId = GitRepoId.CreateUnique();

    //The projects that the scan of the updated clone finds
    private static readonly List<ScannedGitProject> ScannedProjects =
    [
        new(@"Sub.RepoA\AppA", "AppA.csproj", ["LibA", "LibB"]), new(@"Sub.RepoA\LibA", "LibA.csproj", [])
    ];

    //What the handler did after the git commands, in order
    private readonly List<string> _calls = [];

    //Strict: every git command the handler runs must be set up by the test
    private readonly Mock<IGitClient> _gitClient = new(MockBehavior.Strict);
    private readonly Mock<IGitProjectFilesScanner> _gitProjectFilesScanner = new(MockBehavior.Strict);
    private readonly Mock<IGitRepoProjectRepository> _gitRepoProjectRepository = new(MockBehavior.Strict);
    private readonly Mock<IGitsWorkFolder> _gitsWorkFolder = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private List<GitRepoProject>? _storedProjects;

    public UpdateGitProjectCommandHandlerTests()
    {
        _gitsWorkFolder.Setup(f => f.GetProjectFolderPath("Sub/RepoA")).Returns(ProjectPath);
        _gitProjectFilesScanner.Setup(s => s.Scan(ProjectPath)).Returns(() =>
        {
            _calls.Add("Scan");
            return ScannedProjects;
        });
        _gitRepoProjectRepository
            .Setup(r => r.Replace(It.IsAny<GitRepoId>(), It.IsAny<IEnumerable<GitRepoProject>>(),
                It.IsAny<CancellationToken>()))
            .Callback<GitRepoId, IEnumerable<GitRepoProject>, CancellationToken>((gitRepoId, projects, _) =>
            {
                _calls.Add($"Replace {gitRepoId.Value}");
                _storedProjects = [.. projects];
            }).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Callback(() => _calls.Add("Save"))
            .Returns(Task.CompletedTask);
    }

    private UpdateGitProjectCommandHandler CreateHandler(IGitsWorkFolder? gitsWorkFolder = null,
        IGitProjectFilesScanner? gitProjectFilesScanner = null)
    {
        return new UpdateGitProjectCommandHandler(gitsWorkFolder ?? _gitsWorkFolder.Object, _gitClient.Object,
            gitProjectFilesScanner ?? _gitProjectFilesScanner.Object, _gitRepoProjectRepository.Object,
            _unitOfWork.Object);
    }

    private Task<Result> Handle()
    {
        return CreateHandler().Handle(new UpdateGitProjectCommand(RepoAId, "RepoA", Address, "Sub/RepoA"),
            CancellationToken.None);
    }

    //The folder exists, was cloned from the same address and has no local changes
    private void GivenACleanClone()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns(Address);
        _gitClient.Setup(c => c.HasChanges(ProjectPath)).Returns(false);
    }

    //A failed update or scan keeps the projects stored before
    private void AssertNothingIsStored()
    {
        _gitRepoProjectRepository.Verify(
            r => r.Replace(It.IsAny<GitRepoId>(), It.IsAny<IEnumerable<GitRepoProject>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void AssertTheScannedProjectsAreStored()
    {
        Assert.Equal(["Scan", $"Replace {RepoAId.Value}", "Save"], _calls);
        Assert.NotNull(_storedProjects);
        Assert.All(_storedProjects, x => Assert.Equal(RepoAId, x.GitRepoId));
        Assert.Equal(ScannedProjects.Select(x => (x.ProjectRelativePath, x.ProjectFileName,
                string.Join(",", x.DependsOnProjectNames))),
            _storedProjects.Select(x => (x.ProjectRelativePath, x.ProjectFileName,
                string.Join(",", x.Dependencies.Select(d => d.ProjectName)))));
    }

    //Like the client's cache, a folder relative to the SPA project is kept in the Gits folder under the git's name
    [Fact]
    public async Task Handle_UsesTheGitNameAsTheFolderName_WhenTheFolderIsRelativeToTheSpaProject()
    {
        _gitsWorkFolder.Setup(f => f.GetProjectFolderPath("RepoA")).Returns(ProjectPath);
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(false);
        _gitClient.Setup(c => c.Clone(Address, ProjectPath)).Returns(Result.Success());

        Result result = await CreateHandler().Handle(
            new UpdateGitProjectCommand(RepoAId, "RepoA", Address, @"{SpaProjectFolderRelativePath}\src\carcass"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gitsWorkFolder.Verify(f => f.GetProjectFolderPath("RepoA"), Times.Once);
        AssertTheScannedProjectsAreStored();
    }

    //The real work folder: "." is the Gits folder itself and ".." the work folder around it. The strict git client
    //has no setups, so any git command would throw
    [Theory]
    [InlineData(".", "Gits")]
    [InlineData("..", "")]
    public async Task Handle_NeitherDeletesNorClones_WhenTheFolderIsNotInsideTheGitsFolder(string folderName,
        string existingFolder)
    {
        using var temp = new TempFolder();
        string workFolder = temp.Combine("Work");
        string existingFile = Path.Combine(workFolder, existingFolder, "existing.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(existingFile)!);
        await File.WriteAllTextAsync(existingFile, "existing");
        UpdateGitProjectCommandHandler handler = CreateHandler(NewGitsWorkFolder(workFolder));

        Result result = await handler.Handle(new UpdateGitProjectCommand(RepoAId, "RepoA", Address, folderName),
            CancellationToken.None);

        Assert.Equal(GitProjectsErrors.FolderIsOutsideGitsFolder(folderName), result.Error);
        Assert.True(File.Exists(existingFile));
        AssertNothingIsStored();
    }

    [Fact]
    public async Task Handle_ReturnsTheError_WhenTheProjectFolderPathCannotBeCounted()
    {
        _gitsWorkFolder.Setup(f => f.GetProjectFolderPath("Sub/RepoA")).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        AssertNothingIsStored();
    }

    [Fact]
    public async Task Handle_ClonesTheProjectAndStoresItsProjects_WhenTheFolderDoesNotExist()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(false);
        _gitClient.Setup(c => c.Clone(Address, ProjectPath)).Returns(Result.Success());

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        _gitClient.Verify(c => c.Clone(Address, ProjectPath), Times.Once);
        AssertTheScannedProjectsAreStored();
    }

    [Fact]
    public async Task Handle_ReturnsTheCloneError()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(false);
        _gitClient.Setup(c => c.Clone(Address, ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        _gitProjectFilesScanner.Verify(s => s.Scan(It.IsAny<string>()), Times.Never);
        AssertNothingIsStored();
    }

    [Fact]
    public async Task Handle_ReturnsTheError_WhenTheRemoteOriginUrlCannotBeRead()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        AssertNothingIsStored();
    }

    [Fact]
    public async Task Handle_DeletesAndReclonesTheFolder_WhenItWasClonedFromAnotherAddress()
    {
        _gitsWorkFolder.SetupSequence(f => f.Exists(ProjectPath)).Returns(true).Returns(false);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns("git@github.com:test/Other.git");
        _gitsWorkFolder.Setup(f => f.Delete(ProjectPath));
        _gitClient.Setup(c => c.Clone(Address, ProjectPath)).Returns(Result.Success());

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        _gitsWorkFolder.Verify(f => f.Delete(ProjectPath), Times.Once);
        _gitClient.Verify(c => c.Clone(Address, ProjectPath), Times.Once);
        AssertTheScannedProjectsAreStored();
    }

    [Fact]
    public async Task Handle_ReturnsTheError_WhenTheStatusCannotBeRead()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns(Address);
        _gitClient.Setup(c => c.HasChanges(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        AssertNothingIsStored();
    }

    [Fact]
    public async Task Handle_RestoresTheFolderWithoutPulling_WhenItHasLocalChanges()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns(Address);
        _gitClient.Setup(c => c.HasChanges(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.Restore(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        _gitClient.Verify(c => c.Restore(ProjectPath), Times.Once);
        AssertNothingIsStored();
    }

    [Fact]
    public async Task Handle_StoresTheProjectsOfTheRestoredFolder()
    {
        _gitsWorkFolder.Setup(f => f.Exists(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.GetRemoteOriginUrl(ProjectPath)).Returns(Address);
        _gitClient.Setup(c => c.HasChanges(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.Restore(ProjectPath)).Returns(Result.Success());

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        AssertTheScannedProjectsAreStored();
    }

    [Fact]
    public async Task Handle_ReturnsTheRemoteUpdateError()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        AssertNothingIsStored();
    }

    [Fact]
    public async Task Handle_ReturnsTheError_WhenTheNeedForPullCannotBeCounted()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        AssertNothingIsStored();
    }

    [Fact]
    public async Task Handle_PullsAndReturnsThePullResult_WhenPullIsNeeded()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.Pull(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        _gitClient.Verify(c => c.Pull(ProjectPath), Times.Once);
        AssertNothingIsStored();
    }

    [Fact]
    public async Task Handle_StoresTheProjectsOfThePulledFolder()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(true);
        _gitClient.Setup(c => c.Pull(ProjectPath)).Returns(Result.Success());

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        AssertTheScannedProjectsAreStored();
    }

    //A clone that is already up to date is scanned as well: the projects of a repository that was cloned before B9
    //are stored by the first refresh
    [Fact]
    public async Task Handle_StoresTheProjects_WhenTheProjectIsUpToDate()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(false);

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        _gitClient.Verify(c => c.RemoteUpdate(ProjectPath), Times.Once);
        _gitsWorkFolder.Verify(f => f.Delete(It.IsAny<string>()), Times.Never);
        AssertTheScannedProjectsAreStored();
    }

    [Fact]
    public async Task Handle_ReturnsTheScanError_AndKeepsTheStoredProjects()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(false);
        _gitProjectFilesScanner.Setup(s => s.Scan(ProjectPath)).Returns(TestError);

        Result result = await Handle();

        Assert.Equal(TestError, result.Error);
        AssertNothingIsStored();
    }

    //A clone without project files leaves the repository without stored projects
    [Fact]
    public async Task Handle_RemovesTheStoredProjects_WhenTheCloneHasNoProjectFiles()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(false);
        _gitProjectFilesScanner.Setup(s => s.Scan(ProjectPath)).Returns(new List<ScannedGitProject>());

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        Assert.NotNull(_storedProjects);
        Assert.Empty(_storedProjects);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesTheCancellationTokenToTheRepositoryAndTheSave()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(false);
        using var cancellationTokenSource = new CancellationTokenSource();

        await CreateHandler().Handle(new UpdateGitProjectCommand(RepoAId, "RepoA", Address, "Sub/RepoA"),
            cancellationTokenSource.Token);

        _gitRepoProjectRepository.Verify(
            r => r.Replace(RepoAId, It.IsAny<IEnumerable<GitRepoProject>>(), cancellationTokenSource.Token),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(cancellationTokenSource.Token), Times.Once);
    }

    //The longest values that fit into the columns are stored
    [Fact]
    public async Task Handle_StoresTheValuesThatFitIntoTheirColumns()
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(false);
        var longest = new ScannedGitProject(new string('p', GitRepoProject.ProjectRelativePathMaxLength),
            new string('f', GitRepoProject.ProjectFileNameMaxLength),
            [new string('d', GitRepoProjectDependency.ProjectNameMaxLength)]);
        _gitProjectFilesScanner.Setup(s => s.Scan(ProjectPath)).Returns(new List<ScannedGitProject> { longest });

        Result result = await Handle();

        Assert.True(result.IsSuccess);
        Assert.Equal(longest.ProjectRelativePath, Assert.Single(_storedProjects!).ProjectRelativePath);
    }

    //A value that does not fit into its column is an error that names the project file; the stored projects stay
    [Theory]
    [InlineData(GitRepoProject.ProjectRelativePathMaxLength + 1, 1, 1, "ProjectRelativePath",
        GitRepoProject.ProjectRelativePathMaxLength)]
    [InlineData(1, GitRepoProject.ProjectFileNameMaxLength + 1, 1, "ProjectFileName",
        GitRepoProject.ProjectFileNameMaxLength)]
    [InlineData(1, 1, GitRepoProjectDependency.ProjectNameMaxLength + 1, "DependsOnProjectNames",
        GitRepoProjectDependency.ProjectNameMaxLength)]
    public async Task Handle_RefusesAValueThatDoesNotFitIntoItsColumn(int relativePathLength, int fileNameLength,
        int dependencyLength, string valueName, int maxLength)
    {
        GivenACleanClone();
        _gitClient.Setup(c => c.RemoteUpdate(ProjectPath)).Returns(Result.Success());
        _gitClient.Setup(c => c.NeedPull(ProjectPath)).Returns(false);
        var tooLong = new ScannedGitProject(new string('p', relativePathLength), new string('f', fileNameLength),
            ["LibA", new string('d', dependencyLength)]);
        _gitProjectFilesScanner.Setup(s => s.Scan(ProjectPath))
            .Returns(new List<ScannedGitProject> { ScannedProjects[1], tooLong });

        Result result = await Handle();

        Assert.Equal(
            SupportToolsServerApiClientErrors.ValueTooLong(
                $@"{tooLong.ProjectRelativePath}\{tooLong.ProjectFileName}.{valueName}", maxLength), result.Error);
        AssertNothingIsStored();
    }

    //The real work folder and scanner: the clone of a git whose folder is relative to the SPA project is the folder
    //named after the git, and a nested folder name is flattened, so the relative paths start with that folder
    [Theory]
    [InlineData(@"{SpaProjectFolderRelativePath}\src\carcass", "RepoA")]
    [InlineData(@"Libs\RepoA", "Libs.RepoA")]
    [InlineData("Libs/RepoA", "Libs.RepoA")]
    public async Task Handle_ScansTheCloneInTheFolderOfTheClientCache(string gitProjectFolderName,
        string gitsFolderName)
    {
        using var temp = new TempFolder();
        string workFolder = temp.Combine("Work");
        string clonePath = Path.Combine(workFolder, "Gits", gitsFolderName);
        _gitClient.Setup(c => c.Clone(Address, clonePath)).Returns(() =>
        {
            ProjectFiles.Write(clonePath, Path.Combine("AppA", "AppA.csproj"),
                ProjectFiles.Project(@"..\LibA\LibA.csproj"));
            ProjectFiles.Write(clonePath, Path.Combine("LibA", "LibA.csproj"), ProjectFiles.Project());
            return Result.Success();
        });
        UpdateGitProjectCommandHandler handler =
            CreateHandler(NewGitsWorkFolder(workFolder), new GitProjectFilesScanner());

        Result result = await handler.Handle(new UpdateGitProjectCommand(RepoAId, "RepoA", Address,
            gitProjectFolderName), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [($@"{gitsFolderName}\AppA", "AppA.csproj", "LibA"), ($@"{gitsFolderName}\LibA", "LibA.csproj", "")],
            _storedProjects!.Select(x => (x.ProjectRelativePath, x.ProjectFileName,
                string.Join(",", x.Dependencies.Select(d => d.ProjectName)))));
    }

    private static GitsWorkFolder NewGitsWorkFolder(string workFolder)
    {
        return new GitsWorkFolder(MsOptions.Create(new AppOptions { WorkFolder = workFolder }),
            NullLogger<GitsWorkFolder>.Instance);
    }
}
