using Microsoft.EntityFrameworkCore;
using SupportToolsServer.Domain.GitIgnoreFileTypes;

namespace SupportToolsServer.Application.Data;

public interface ISupportToolsServerDbContext
{
    DbSet<GitIgnoreFileType> GitIgnoreFileTypes { get; set; }
}