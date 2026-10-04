using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Application.Registry;
using SupportToolsServerApiContracts.Errors;
using SupportToolsServerCore.Domain.ReactAppTemplates;
using SystemTools.Application.Abstractions.Messaging;
using SystemTools.Domain.Abstractions;
using SystemTools.SharedKernel;

namespace SupportToolsServer.Application.ReactAppTemplates.DeleteReactAppTemplate;

public sealed class DeleteReactAppTemplateCommandHandler : ICommandHandler<DeleteReactAppTemplateCommand>
{
    private readonly IReactAppTemplateRepository _reactAppTemplateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReactAppTemplateCommandHandler(IReactAppTemplateRepository reactAppTemplateRepository,
        IUnitOfWork unitOfWork)
    {
        _reactAppTemplateRepository = reactAppTemplateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteReactAppTemplateCommand command, CancellationToken cancellationToken)
    {
        ReactAppTemplate? reactAppTemplate =
            await _reactAppTemplateRepository.GetByName(command.Name, cancellationToken);
        if (reactAppTemplate is null)
        {
            return SupportToolsServerApiClientErrors.RecordWithNameNotFound(ReactAppTemplateContractMapper.EntityName,
                command.Name);
        }

        if (command.Version is not null && command.Version.Value != reactAppTemplate.Version)
        {
            return SupportToolsServerApiClientErrors.ConcurrencyConflict(ReactAppTemplateContractMapper.EntityName,
                command.Name, command.Version.Value, reactAppTemplate.Version);
        }

        //აქ მოწმდება, ხომ არ მიმართავს ჩანაწერს სხვა აგრეგატი (409 RecordIsInUse მომხმარებლების სიით).
        //ReactAppTemplate-ს ჯერ არავინ მიმართავს: B5 (ProjectTemplate.ReactTemplateName) FK-ს Restrict-ით დაამატებს და
        //შაბლონების შემოწმებას აქ ჩასვამს

        _reactAppTemplateRepository.Delete(reactAppTemplate);
        return await RecordVersions.SaveChanges(_unitOfWork, ReactAppTemplateContractMapper.EntityName, command.Name,
            reactAppTemplate.Version,
            async ct => (await _reactAppTemplateRepository.GetByName(command.Name, ct))?.Version, cancellationToken);
    }
}
