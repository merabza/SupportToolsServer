//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace SupportToolsServer.Domain.GitIgnoreFileTypes;

//public class GitIgnoreFileTypeUpSyncer
//{
//    private readonly IGitIgnoreFileTypeRepository _gitIgnoreFileTypeRepository;
//    private readonly List<GitIgnoreFileType> _requestUploadGitIgnoreFileTypes;

//    public GitIgnoreFileTypeUpSyncer(IGitIgnoreFileTypeRepository gitIgnoreFileTypeRepository,
//        List<GitIgnoreFileType> requestUploadGitIgnoreFileTypes)
//    {
//        _gitIgnoreFileTypeRepository = gitIgnoreFileTypeRepository;
//        _requestUploadGitIgnoreFileTypes = requestUploadGitIgnoreFileTypes;
//    }

//    public async Task<bool> DoSyncUp(bool merge = true, CancellationToken cancellationToken = default)
//    {
//        //ჩავტვირთოთ ბაზაში არსებული ყველა ჩანაწერი
//        var existingGitIgnoreFileTypes = await _gitIgnoreFileTypeRepository.GetAll(cancellationToken);

//        //ვიპოვით ბაზაში ყველა ისეთი ჩანაწერი, რომელიც არ გვაქვს მოწოდებულ სიაში
//        var gitIgnoreFileTypesToDelete = existingGitIgnoreFileTypes.Where(e =>
//            _requestUploadGitIgnoreFileTypes.All(r => r.Id != e.Id)).ToList();
//        //წავშალოთ ისინი
//        gitIgnoreFileTypesToDelete.ForEach(e => _gitIgnoreFileTypeRepository.Delete(e));

//        //ვიპოვით ყველა ჩანაწერი, რომელიც მოწოდებულ სიაშია, მაგრამ ბაზაში არ არსებობს
//        var gitIgnoreFileTypesToAdd = _requestUploadGitIgnoreFileTypes.Where(r =>
//            existingGitIgnoreFileTypes.All(e => e.Id != r.Id)).ToList();
//        //დავამატოთ ისინი
//        gitIgnoreFileTypesToAdd.ForEach(r => _gitIgnoreFileTypeRepository.Add(r));

//        //ვიპოვოთ ყველა ჩანაწერი, რომელიც ორივე სიაშია და შევადაროთ მათი შინაარსი
//        var gitIgnoreFileTypesToUpdate = _requestUploadGitIgnoreFileTypes.Where(r =>
//            existingGitIgnoreFileTypes.Any(e => e.Id == r.Id)).ToList();
//        //განვაახლოთ ისინი
//        gitIgnoreFileTypesToUpdate.ForEach(r => _gitIgnoreFileTypeRepository.Update(r));

//        return true;
//    }
//}