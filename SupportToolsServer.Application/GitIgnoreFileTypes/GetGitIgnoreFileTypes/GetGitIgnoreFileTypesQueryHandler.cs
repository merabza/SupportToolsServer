using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerCore.Domain.GitIgnoreFileTypes;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.GetGitIgnoreFileTypes;

public class
    GetGitIgnoreFileTypesQueryHandler : IQueryHandler<GetGitIgnoreFileTypesQuery, List<StsGitIgnoreFileTypeDataModel>>
{
    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;

    public GetGitIgnoreFileTypesQueryHandler(IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository)
    {
        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
    }

    public async Task<Result<List<StsGitIgnoreFileTypeDataModel>>> Handle(GetGitIgnoreFileTypesQuery query,
        CancellationToken cancellationToken)
    {
        List<GitIgnoreFileType> gitIgnoreFileTypes = await _gitIgnoreFileTypeRepository.GetAll(cancellationToken);

        List<StsGitIgnoreFileTypeDataModel> gitIgnoreFileTypeModels =
        [
            .. gitIgnoreFileTypes.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x =>
                new StsGitIgnoreFileTypeDataModel { Id = x.Id.Value, Name = x.Name, Content = x.Content })
        ];
        return gitIgnoreFileTypeModels;
    }
}
