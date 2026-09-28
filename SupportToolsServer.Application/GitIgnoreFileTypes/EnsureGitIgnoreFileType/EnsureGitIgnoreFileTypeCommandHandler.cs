using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.EnsureGitIgnoreFileType;

public class EnsureGitIgnoreFileTypeCommandHandler : ICommandHandler<EnsureGitIgnoreFileTypeCommand>
{
    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EnsureGitIgnoreFileTypeCommandHandler(IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(EnsureGitIgnoreFileTypeCommand command, CancellationToken cancellationToken)
    {
        GitIgnoreFileType? existing = await _gitIgnoreFileTypeRepository.GetByName(command.Name, cancellationToken);
        if (existing is not null)
        {
            return Result.Success();
        }

        _gitIgnoreFileTypeRepository.Add(new GitIgnoreFileType(GitIgnoreFileTypeId.CreateUnique(), command.Name,
            string.Empty));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
