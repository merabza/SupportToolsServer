using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf;
using SystemToolsShared.Errors;
using WebInstallers;

namespace SupportToolsServerApplication.Services.GitIgnoreFileTypes.Delete;

public class GitIgnoreFileTypeDeleteService : IScopedService
{
    public Task<OneOf<Unit, Err[]>> DeleteGitIgnoreFileType(string requestRecordKey, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
