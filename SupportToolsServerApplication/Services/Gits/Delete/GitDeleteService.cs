using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf;
using SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Services.Gits.Delete;

public class GitDeleteService : IScopedServiceSupportToolsServerApplication
{
    public Task<OneOf<Unit, Err[]>> DeleteGitRepo(string requestRecordKey, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}