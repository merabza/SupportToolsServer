using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SupportToolsServerCore.Domain.GitRepos;
using SupportToolsServerCore.Domain.Sync;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.SyncUp;

public class SyncUpGitIgnoreFileTypesCommandHandler : ICommandHandler<SyncUpGitIgnoreFileTypesCommand>
{
    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;
    private readonly IGitRepoRepository _gitRepoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SyncUpGitIgnoreFileTypesCommandHandler(IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository,
        IGitRepoRepository gitRepoRepository, IUnitOfWork unitOfWork)
    {
        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
        _gitRepoRepository = gitRepoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SyncUpGitIgnoreFileTypesCommand request, CancellationToken cancellationToken)
    {
        //ატვირთული ჩანაწერი არსებულს სახელით ემთხვევა და სერვერის Id-ს ინარჩუნებს, რადგან git რეპოზიტორიები
        //gitignore ფაილის ტიპს Id-ით მიმართავენ. კლიენტის მიერ გამოგზავნილი Id არ გამოიყენება
        List<GitIgnoreFileType> existingGitIgnoreFileTypes =
            await _gitIgnoreFileTypeRepository.GetAll(cancellationToken);
        Dictionary<string, GitIgnoreFileTypeId> existingIds =
            existingGitIgnoreFileTypes.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);

        if (!request.Merge)
        {
            //სიაში არარსებული ჩანაწერები წაიშლება, ამიტომ არც ერთს არ უნდა იყენებდეს რეპოზიტორია
            HashSet<string> uploadedNames =
                new(request.UploadGitIgnoreFileTypes.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
            Result notUsedResult = GitIgnoreFileTypeDeletion.CheckNotUsed(
                existingGitIgnoreFileTypes.Where(x => !uploadedNames.Contains(x.Name)),
                await _gitRepoRepository.GetAll(cancellationToken));
            if (notUsedResult.IsFailure)
            {
                return notUsedResult;
            }
        }

        var syncer = new Syncroniser<GitIgnoreFileType, GitIgnoreFileTypeId>(_gitIgnoreFileTypeRepository, [
            .. request.UploadGitIgnoreFileTypes.Select(s =>
                new GitIgnoreFileType(existingIds.GetValueOrDefault(s.Name) ?? GitIgnoreFileTypeId.CreateUnique(),
                    s.Name, s.Content))
        ]);
        await syncer.DoSyncUp(request.Merge, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
