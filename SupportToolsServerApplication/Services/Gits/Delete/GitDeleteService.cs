using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf;
using SystemToolsShared.Errors;
using WebInstallers;

namespace SupportToolsServerApplication.Services.Gits.Delete;

public class GitDeleteService : IScopedService
{
    public Task<OneOf<Unit, Err[]>> DeleteGitRepo(string requestRecordKey, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
