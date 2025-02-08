using System;

namespace SupportToolsServerDb.Models;

public sealed class GitData
{
    public int Id { get; set; }
    public required string GitAddress { get; set; }
    public required string Name { get; set; }
    public int GitIgnoreFileTypeId { get; set; }

    private GitIgnoreFileType? _gitIgnoreFileType;
    public GitIgnoreFileType GitIgnoreFileTypeNavigation
    {
        get =>
            _gitIgnoreFileType ??
            throw new InvalidOperationException("Uninitialized property: " + nameof(GitIgnoreFileType));
        set => _gitIgnoreFileType = value;
    }
}