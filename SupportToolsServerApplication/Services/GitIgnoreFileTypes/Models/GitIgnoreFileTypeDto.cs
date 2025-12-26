using System;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.Models;

public class GitIgnoreFileTypeDto
{
    public Guid RowId { get; set; }
    public required string Name { get; set; }
    public required string Content { get; set; }
}