using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SupportToolsServerCore.Domain.Projects;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.EditorConfigFileTypes.DeleteEditorConfigFileType;

public class DeleteEditorConfigFileTypeCommandHandler : ICommandHandler<DeleteEditorConfigFileTypeCommand>
{
    private readonly IEditorConfigFileTypeRepository _editorConfigFileTypeRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEditorConfigFileTypeCommandHandler(IEditorConfigFileTypeRepository editorConfigFileTypeRepository,
        IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _editorConfigFileTypeRepository = editorConfigFileTypeRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteEditorConfigFileTypeCommand command, CancellationToken cancellationToken)
    {
        //სახელი რეგისტრის გარეშე ედრება, როგორც SyncUp-ში და სახელის უნიკალურ ინდექსში
        List<EditorConfigFileType> editorConfigFileTypes =
            await _editorConfigFileTypeRepository.GetAll(cancellationToken);
        EditorConfigFileType? editorConfigFileType = editorConfigFileTypes.Find(x =>
            string.Equals(x.Name, command.Name, StringComparison.OrdinalIgnoreCase));
        if (editorConfigFileType is null)
        {
            return SupportToolsServerApiClientErrors.EditorConfigFileTypeWithNameNotFound(command.Name);
        }

        //შაბლონს პროექტები მიმართავს (FK, Restrict), ამიტომ გამოყენებულს არ ვშლით: 409 RecordIsInUse პროექტების სიით
        Result notUsedResult = EditorConfigFileTypeDeletion.CheckNotUsed([editorConfigFileType],
            await _projectRepository.GetAll(cancellationToken));
        if (notUsedResult.IsFailure)
        {
            return notUsedResult;
        }

        _editorConfigFileTypeRepository.Delete(editorConfigFileType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
