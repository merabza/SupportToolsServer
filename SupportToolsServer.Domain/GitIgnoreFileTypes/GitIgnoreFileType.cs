using SupportToolsServer.Domain.Primitives;

namespace SupportToolsServer.Domain.GitIgnoreFileTypes;

public class GitIgnoreFileType : Entity<GitIgnoreFileTypeId>
{
    public GitIgnoreFileType(GitIgnoreFileTypeId id, string name, string content) : base(id)
    {
        Name = name;
        Content = content;
    }

    public string Name { get; private set; }
    public string Content { get; private set; }
}