using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.Sync;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.EditorConfigFileTypes.SyncUp;

public class SyncUpEditorConfigFileTypesCommandHandler : ICommandHandler<SyncUpEditorConfigFileTypesCommand>
{
    private readonly IEditorConfigFileTypeRepository _editorConfigFileTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SyncUpEditorConfigFileTypesCommandHandler(IEditorConfigFileTypeRepository editorConfigFileTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _editorConfigFileTypeRepository = editorConfigFileTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SyncUpEditorConfigFileTypesCommand request, CancellationToken cancellationToken)
    {
        //კლიენტი Id-ს არ აგზავნის: ატვირთული ჩანაწერი არსებულს სახელით ემთხვევა და სერვერის Id-ს ინარჩუნებს.
        //ასე არსებული სახელის ჩანაწერი ახლდება და სახელის უნიკალურ ინდექსს ახალი ჩანაწერი არ ეჯახება
        List<EditorConfigFileType> existingEditorConfigFileTypes =
            await _editorConfigFileTypeRepository.GetAll(cancellationToken);
        Dictionary<string, EditorConfigFileTypeId> existingIds =
            existingEditorConfigFileTypes.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);

        var syncer = new Syncroniser<EditorConfigFileType, EditorConfigFileTypeId>(_editorConfigFileTypeRepository, [
            .. request.UploadEditorConfigFileTypes.Select(s =>
                new EditorConfigFileType(
                    existingIds.GetValueOrDefault(s.Name) ?? EditorConfigFileTypeId.CreateUnique(), s.Name,
                    s.Content))
        ]);
        await syncer.DoSyncUp(request.Merge, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
