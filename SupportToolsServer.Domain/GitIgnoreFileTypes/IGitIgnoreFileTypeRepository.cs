using SupportToolsServer.Domain.Sync;

namespace SupportToolsServer.Domain.GitIgnoreFileTypes;

public interface IGitIgnoreFileTypeRepository : ICrudRepository<GitIgnoreFileType, GitIgnoreFileTypeId>;