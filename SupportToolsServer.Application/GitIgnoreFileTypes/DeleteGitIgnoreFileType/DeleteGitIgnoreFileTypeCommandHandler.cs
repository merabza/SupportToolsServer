using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.DeleteGitIgnoreFileType;

public class DeleteGitIgnoreFileTypeCommandHandler : ICommandHandler<DeleteGitIgnoreFileTypeCommand>
{
    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;
    private readonly IGitRepoRepository _gitRepoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGitIgnoreFileTypeCommandHandler(IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository,
        IGitRepoRepository gitRepoRepository, IUnitOfWork unitOfWork)
    {
        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
        _gitRepoRepository = gitRepoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteGitIgnoreFileTypeCommand command, CancellationToken cancellationToken)
    {
        GitIgnoreFileType? gitIgnoreFileType =
            await _gitIgnoreFileTypeRepository.GetByName(command.Name, cancellationToken);
        if (gitIgnoreFileType is null)
        {
            return SupportToolsServerApiClientErrors.GitIgnoreFileTypeWithNameNotFound(command.Name);
        }

        Result notUsedResult = GitIgnoreFileTypeDeletion.CheckNotUsed([gitIgnoreFileType],
            await _gitRepoRepository.GetAll(cancellationToken));
        if (notUsedResult.IsFailure)
        {
            return notUsedResult;
        }

        _gitIgnoreFileTypeRepository.Delete(gitIgnoreFileType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
