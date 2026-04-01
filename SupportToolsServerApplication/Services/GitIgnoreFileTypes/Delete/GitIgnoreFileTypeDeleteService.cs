using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf;
using SystemTools.SystemToolsShared.Errors;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.Delete;

public class GitIgnoreFileTypeDeleteService : IScopedServiceSupportToolsServerApplication
{
    public Task<OneOf<Unit, Error[]>> DeleteGitIgnoreFileType(string requestRecordKey,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
