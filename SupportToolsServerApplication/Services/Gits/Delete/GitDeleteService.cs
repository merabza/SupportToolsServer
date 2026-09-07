using System;
using System.Threading;
using System.Threading.Tasks;
using SystemTools.SharedKernel;

namespace SupportToolsServerApplication.Services.Gits.Delete;

public class GitDeleteService : IScopedServiceSupportToolsServerApplication
{
    public Task<Result> DeleteGitRepo(string requestRecordKey, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
