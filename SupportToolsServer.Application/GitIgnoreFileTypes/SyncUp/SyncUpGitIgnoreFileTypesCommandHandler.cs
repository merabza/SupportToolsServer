using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Domain.GitIgnoreFileTypes;
using SupportToolsServer.Domain.Sync;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;

public class SyncUpGitIgnoreFileTypesCommandHandler : ICommandHandler<SyncUpGitIgnoreFileTypesCommand>
{
    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;

    public SyncUpGitIgnoreFileTypesCommandHandler(IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository)
    {
        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
    }

    public async Task<Result> Handle(SyncUpGitIgnoreFileTypesCommand request, CancellationToken cancellationToken)
    {
        var syncer = new Syncroniser<GitIgnoreFileType, GitIgnoreFileTypeId>(_gitIgnoreFileTypeRepository, [
            .. request.UploadGitIgnoreFileTypes.Select(s =>
                new GitIgnoreFileType(new GitIgnoreFileTypeId(s.Id), s.Name, s.Content))
        ]);
        await syncer.DoSyncUp(request.Merge, cancellationToken);
        return Result.Success();
    }
}
