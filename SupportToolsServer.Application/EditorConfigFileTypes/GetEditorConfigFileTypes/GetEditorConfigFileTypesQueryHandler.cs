using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.EditorConfigFileTypes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.EditorConfigFileTypes.GetEditorConfigFileTypes;

public class
    GetEditorConfigFileTypesQueryHandler : IQueryHandler<GetEditorConfigFileTypesQuery,
    List<StsEditorConfigFileTypeDataModel>>
{
    private readonly IEditorConfigFileTypeRepository _editorConfigFileTypeRepository;

    public GetEditorConfigFileTypesQueryHandler(IEditorConfigFileTypeRepository editorConfigFileTypeRepository)
    {
        _editorConfigFileTypeRepository = editorConfigFileTypeRepository;
    }

    public async Task<Result<List<StsEditorConfigFileTypeDataModel>>> Handle(GetEditorConfigFileTypesQuery query,
        CancellationToken cancellationToken)
    {
        List<EditorConfigFileType> editorConfigFileTypes =
            await _editorConfigFileTypeRepository.GetAll(cancellationToken);

        List<StsEditorConfigFileTypeDataModel> editorConfigFileTypeModels =
        [
            .. editorConfigFileTypes.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x =>
                new StsEditorConfigFileTypeDataModel { Name = x.Name, Content = x.Content, Version = x.Version })
        ];
        return editorConfigFileTypeModels;
    }
}
