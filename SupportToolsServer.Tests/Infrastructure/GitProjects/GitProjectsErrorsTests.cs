using SupportToolsServer.Infrastructure.GitProjects;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportToolsServer.Tests.Infrastructure.GitProjects;

public sealed class GitProjectsErrorsTests
{
    [Fact]
    public void WorkFolderIsNotSpecified_IsAProblemWithItsNameAsTheCode()
    {
        Error error = GitProjectsErrors.WorkFolderIsNotSpecified;

        Assert.Equal("WorkFolderIsNotSpecified", error.Code);
        Assert.Equal("AppOptions:WorkFolder is not specified", error.Description);
        Assert.Equal(ErrorType.Problem, error.Type);
    }

    [Fact]
    public void CannotCreateFolder_IsAProblemNamingTheFolder()
    {
        Error error = GitProjectsErrors.CannotCreateFolder(@"C:\Work");

        Assert.Equal("CannotCreateFolder", error.Code);
        Assert.Equal(@"Folder C:\Work does not exist and cannot be created", error.Description);
        Assert.Equal(ErrorType.Problem, error.Type);
    }
}
