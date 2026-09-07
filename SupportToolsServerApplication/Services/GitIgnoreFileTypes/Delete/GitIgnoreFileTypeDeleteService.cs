using System;
using System.Threading;
using System.Threading.Tasks;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.Delete;

public class GitIgnoreFileTypeDeleteService : IScopedServiceSupportToolsServerApplication
{
    public Task<Result> DeleteGitIgnoreFileType(string requestRecordKey, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
