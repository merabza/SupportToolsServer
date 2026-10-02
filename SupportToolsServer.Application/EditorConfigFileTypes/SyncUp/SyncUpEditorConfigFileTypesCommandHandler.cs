using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.Primitives;
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
        //ასე არსებული სახელის ჩანაწერი ახლდება და სახელის უნიკალურ ინდექსს ახალი ჩანაწერი არ ეჯახება.
        //არსებული ჩანაწერი ყოველთვის თავიდან იწერება, ამიტომ მისი ვერსია ერთით იზრდება
        List<EditorConfigFileType> existingEditorConfigFileTypes =
            await _editorConfigFileTypeRepository.GetAll(cancellationToken);
        Dictionary<string, EditorConfigFileType> existingByName =
            existingEditorConfigFileTypes.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        var syncer = new Syncroniser<EditorConfigFileType, EditorConfigFileTypeId>(_editorConfigFileTypeRepository, [
            .. request.UploadEditorConfigFileTypes.Select(s =>
                existingByName.TryGetValue(s.Name, out EditorConfigFileType? existing)
                    ? new EditorConfigFileType(existing.Id, s.Name, s.Content, existing.Version + 1)
                    : new EditorConfigFileType(EditorConfigFileTypeId.CreateUnique(), s.Name, s.Content,
                        EntityVersion.Initial))
        ]);
        await syncer.DoSyncUp(request.Merge, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
